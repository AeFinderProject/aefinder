using AeFinder.App.BlockState;
using AeFinder.App.Handlers;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.State.BlockStates;
using AeFinder.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AeFinder.App.BlockProcessing;

public interface IBlockProcessingService
{
    Task ProcessAsync(string chainId, string branchBlockHash);
}

public class BlockProcessingService : IBlockProcessingService, ITransientDependency
{
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IFullBlockProcessor _fullBlockProcessor;
    private readonly IAppDataIndexManagerProvider _appDataIndexManagerProvider;
    private readonly IAppStateProvider _appStateProvider;
    private readonly IGeneralAppDataIndexProvider _generalAppDataIndexProvider;
    private readonly IRuntimeTypeProvider _runtimeTypeProvider;
    private readonly IAppBlockStateChangeProvider _appBlockStateChangeProvider;
    private readonly ILogger<BlockProcessingService> _logger;
    private readonly BlockProcessingOptions _blockProcessingOptions;
    public ILocalEventBus LocalEventBus { get; set; }

    public BlockProcessingService(IAppBlockStateSetProvider appBlockStateSetProvider,
        IFullBlockProcessor fullBlockProcessor, IAppDataIndexManagerProvider appDataIndexManagerProvider,
        IAppStateProvider appStateProvider, 
        IGeneralAppDataIndexProvider generalAppDataIndexProvider, IRuntimeTypeProvider runtimeTypeProvider,
        IAppBlockStateChangeProvider appBlockStateChangeProvider, ILogger<BlockProcessingService> logger,
        IOptionsSnapshot<BlockProcessingOptions> blockProcessingOptions)
    {
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _fullBlockProcessor = fullBlockProcessor;
        _appDataIndexManagerProvider = appDataIndexManagerProvider;
        _appStateProvider = appStateProvider;
        _generalAppDataIndexProvider = generalAppDataIndexProvider;
        _runtimeTypeProvider = runtimeTypeProvider;
        _appBlockStateChangeProvider = appBlockStateChangeProvider;
        _blockProcessingOptions = blockProcessingOptions.Value;
        _logger = logger;
    }

    public async Task ProcessAsync(string chainId, string branchBlockHash)
    {
        var blockStateSets = await GetToBeProcessedBlockStateSetsAsync(chainId, branchBlockHash);
        if (!await IsProcessAsync(chainId, blockStateSets))
        {
            return;
        }

        var rollbackBlockStateSets = await GetToBeRollbackBlockStateSetsAsync(chainId, blockStateSets);

        var longestChainBlockStateSet = blockStateSets.Last();
        
        foreach (var blockStateSet in rollbackBlockStateSets)
        {
            foreach (var change in blockStateSet.Changes)
            {
                var longestChainState = await _appStateProvider.GetStateAsync(chainId, change.Key,
                    new BlockIndex(longestChainBlockStateSet.Block.BlockHash, longestChainBlockStateSet.Block.BlockHeight));
                
                var type = _runtimeTypeProvider.GetType(change.Value.Type);
                if (longestChainState != null)
                {
                    await _generalAppDataIndexProvider.AddOrUpdateAsync(longestChainState, type);
                }
                else
                {
                    var entity = JsonConvert.DeserializeObject(change.Value.Value, type);
                    await _generalAppDataIndexProvider.DeleteAsync(entity, type);
                }
            }
            await SetBlockStateSetProcessedAsync(chainId, blockStateSet, false);
        }

        var changeKeys = new Dictionary<long, List<BlockStateChange>>();
        foreach (var blockStateSet in blockStateSets)
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(_blockProcessingOptions.MaxBlockProcessingTime);
                await _fullBlockProcessor.ProcessAsync(blockStateSet.Block, cts.Token);
            }

            await SetBlockStateSetProcessedAsync(chainId, blockStateSet, true);

            try
            {
                changeKeys[blockStateSet.Block.BlockHeight] = blockStateSet.Changes
                    .Select(o => new BlockStateChange { Key = o.Key, Type = o.Value.Type }).ToList();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "BlockStateSet exception. ChainId: {ChainId}, BlockStateSet: {BlockStateSet}", chainId,
                    JsonConvert.SerializeObject(blockStateSet));
                throw;
            }

        }
        
        await _appBlockStateChangeProvider.AddBlockStateChangeAsync(chainId, changeKeys);

        await _appBlockStateSetProvider.SetBestChainBlockStateSetAsync(chainId,
            longestChainBlockStateSet.Block.BlockHash);
        await _appBlockStateSetProvider.SaveDataAsync(chainId);
        
        await _appDataIndexManagerProvider.SavaDataAsync();

        if (longestChainBlockStateSet.Block.Confirmed)
        {
            await LocalEventBus.PublishAsync(new LastIrreversibleBlockStateSetFoundEventData
            {
                ChainId = chainId,
                BlockHash = longestChainBlockStateSet.Block.BlockHash,
                BlockHeight = longestChainBlockStateSet.Block.BlockHeight
            });
        }
    }
    
    private async Task<List<BlockStateSet>> GetToBeProcessedBlockStateSetsAsync(string chainId, string branchBlockHash)
    {
        var blockStateSets = new List<BlockStateSet>();

        var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, branchBlockHash);
        while (blockStateSet != null && !blockStateSet.Processed)
        {
            blockStateSets.Add(blockStateSet);
            blockStateSet =
                await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, blockStateSet.Block.PreviousBlockHash);
        }

        blockStateSets.Reverse();
        return blockStateSets;
    }
    
    private async Task<List<BlockStateSet>> GetToBeRollbackBlockStateSetsAsync(string chainId, List<BlockStateSet> toExecuteBlockStateSets)
    {
        var rollbackBlockStateSets = new List<BlockStateSet>();
        var bestChainBlockStateSet = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
        if (bestChainBlockStateSet == null)
        {
            return rollbackBlockStateSets;
        }

        var toExecutePreviousBlockHashes = new HashSet<string>();
        foreach (var l in toExecuteBlockStateSets)
        {
            toExecutePreviousBlockHashes.Add(l.Block.PreviousBlockHash);
        }

        var blockHash = bestChainBlockStateSet.Block.BlockHash;
        while (!toExecutePreviousBlockHashes.Contains(blockHash))
        {
            var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, blockHash);
            if (blockStateSet == null)
            {
                break;
            }
            rollbackBlockStateSets.Add(blockStateSet);
            blockHash = blockStateSet.Block.PreviousBlockHash;
        }

        return rollbackBlockStateSets;
    }
    
    private async Task<bool> IsProcessAsync(string chainId, List<BlockStateSet> blockStateSets)
    {
        if (blockStateSets.Count == 0)
        {
            return false;
        }

        return await IsInLibBranchAsync(chainId, blockStateSets.First().Block.BlockHash);
    }
    
    private async Task<bool> IsInLibBranchAsync(string chainId, string blockHash)
    {
        var lastIrreversibleBlockStateSet = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
        if (lastIrreversibleBlockStateSet == null)
        {
            return true;
        }

        while (true)
        {
            var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, blockHash);
            if (blockStateSet == null || blockStateSet.Block.BlockHeight < lastIrreversibleBlockStateSet.Block.BlockHeight)
            {
                return false;
            }

            if (blockStateSet.Block.Confirmed)
            {
                return true;
            }
            blockHash = blockStateSet.Block.PreviousBlockHash;
        }
    }
    
    private async Task SetBlockStateSetProcessedAsync(string chainId, BlockStateSet blockStateSet, bool processed)
    {
        blockStateSet.Processed = processed;
        if (!processed)
        {
            blockStateSet.Changes.Clear();
        }

        await _appBlockStateSetProvider.UpdateBlockStateSetAsync(chainId, blockStateSet);
    }
}