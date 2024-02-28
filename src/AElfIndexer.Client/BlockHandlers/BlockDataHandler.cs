using AElf.Types;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.BlockState;
using AElfIndexer.Grains.Grain.BlockState;
using AElfIndexer.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.BlockHandlers;

public class BlockDataHandler : IBlockDataHandler, ITransientDependency 
{
    private readonly IAppStateProvider _appStateProvider;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppDataIndexManagerProvider _appDataIndexManagerProvider;
    protected readonly IObjectMapper ObjectMapper;
    private readonly IFullBlockProcessor _fullBlockProcessor;
    private readonly AppInfoOptions _appInfoOptions;
    
    protected readonly ILogger<BlockDataHandler> Logger;
    
    public ILocalEventBus LocalEventBus { get; set; }
    
    protected BlockDataHandler(IObjectMapper objectMapper,
        ILogger<BlockDataHandler> logger,
        IAppStateProvider appStateProvider, IAppBlockStateSetProvider appBlockStateSetProvider,
        IAppDataIndexManagerProvider appDataIndexManagerProvider, IFullBlockProcessor fullBlockProcessor, IOptionsSnapshot<AppInfoOptions> appInfoOptions)
    {
        ObjectMapper = objectMapper;
        Logger = logger;
        _appStateProvider = appStateProvider;
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _appDataIndexManagerProvider = appDataIndexManagerProvider;
        _fullBlockProcessor = fullBlockProcessor;
        _appInfoOptions = appInfoOptions.Value;
    }
    
    public async Task HandleBlockChainDataAsync(string chainId, List<BlockWithTransactionDto> blockDtos)
    {
        var libHeight = 0L;
        var libHash = string.Empty;

        await _appBlockStateSetProvider.InitializeAsync(chainId);
        
        var longestChainBlockStateSet = await _appBlockStateSetProvider.GetLongestChainBlockStateSetAsync(chainId);
        if (longestChainBlockStateSet != null && !longestChainBlockStateSet.Processed)
        {
            Logger.LogDebug("Handle unfinished longest chain data. ChainId: {ChainId}, ClientId: {ClientId}, Version: {Version}", chainId, _appInfoOptions.AppId, _appInfoOptions.Version);
            await ProcessUnfinishedLongestChainAsync(chainId, longestChainBlockStateSet);
        }

        var blockStateSets = await _appBlockStateSetProvider.GetBlockStateSetsAsync(chainId);
        var longestChain = new List<BlockStateSet>();
        
        var libBlockHeight = blockStateSets.Count != 0 ? blockStateSets.Min(b => b.Value.Block.BlockHeight) : 0;
        libBlockHeight = libBlockHeight == 1 ? 0 : libBlockHeight;
        var longestHeight = blockStateSets.Count != 0 ? blockStateSets.Max(b => b.Value.Block.BlockHeight) : 0;
        Logger.LogDebug(
            "Handle block data. ChainId: {ChainId}, ClientId: {ClientId}, Version: {Version}, Lib Height: {Lib}, Longest Chain Height: {LongestHeight}",
            chainId, _appInfoOptions.AppId, _appInfoOptions.Version, libBlockHeight, longestHeight);

        foreach (var blockDto in blockDtos)
        {
            // Skip if block height less than lib block height
            if(blockDto.BlockHeight <= libBlockHeight)
            {
                continue;
            }

            if (blockStateSets.Count != 0 && !blockStateSets.ContainsKey(blockDto.PreviousBlockHash) && blockDto.PreviousBlockHash!= Hash.Empty.ToHex())
            {
                Logger.LogWarning(
                    $"Previous block {blockDto.PreviousBlockHash} not found. blockHeight: {blockDto.BlockHeight}, blockStateSets max block height: {blockStateSets.Max(b => b.Value.Block.BlockHeight)}");
                continue;
            }

            if (blockStateSets.TryGetValue(blockDto.BlockHash, out var blockStateSet) && blockStateSet.Processed &&
                blockDto.Confirmed)
            {
                await DealWithConfirmBlockAsync(blockDto, blockStateSet);
                libHeight = blockDto.BlockHeight;
                libHash = blockDto.BlockHash;
                continue;
            }

            // Skip if blockStateSets contain block and processed
            if (blockStateSets.TryGetValue(blockDto.BlockHash, out blockStateSet) && blockStateSet.Processed)
            {
                continue;
            }

            if (blockStateSet == null)
            {
                blockStateSet = new BlockStateSet
                {
                    Block = blockDto,
                    Changes = new(),
                };
                await _appBlockStateSetProvider.AddBlockStateSetAsync(chainId, blockStateSet);
                blockStateSets = await _appBlockStateSetProvider.GetBlockStateSetsAsync(chainId);
            }
            if (longestChainBlockStateSet == null || blockDto.PreviousBlockHash == longestChainBlockStateSet.Block.BlockHash)
            {
                longestChain.Add(blockStateSet);
                longestChainBlockStateSet = blockStateSet;
                await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId, blockStateSet.Block.BlockHash);
            }
            else if (blockDto.BlockHeight > longestChainBlockStateSet.Block.BlockHeight)
            {
                var targetLongestChain = GetLongestChain(blockStateSets, blockDto.BlockHash);
                if (targetLongestChain.Count > 0)
                {
                    longestChain = targetLongestChain;
                    longestChainBlockStateSet = blockStateSet;
                    await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId, blockStateSet.Block.BlockHash);
                }
                else
                {
                    Logger.LogWarning("Longest chain not found.");
                }
            }
        }

        Logger.LogDebug("Handle longest chain data. ChainId: {ChainId}, ClientId: {ClientId}, Version: {Version}", chainId, _appInfoOptions.AppId, _appInfoOptions.Version);
        if (longestChain.Count > 0)
        {
            await _appBlockStateSetProvider.SaveDataAsync(chainId);
            await ProcessLongestChainAsync(chainId, longestChain, blockStateSets);
            
            var confirmBlock = longestChain.LastOrDefault(b => b.Block.Confirmed);
            if (confirmBlock != null && confirmBlock.Block.BlockHeight > libHeight)
            {
                libHeight = confirmBlock.Block.BlockHeight;
                libHash = confirmBlock.Block.BlockHash;
            }
        }
        
        //Clean block state sets under latest lib block
        Logger.LogDebug("Handle lib data. ChainId: {ChainId}, ClientId: {ClientId}, Version: {Version}, Lib height: {LibHeight}", chainId, _appInfoOptions.AppId, _appInfoOptions.Version, libHeight);
        if (libHeight != 0)
        {
            await _appBlockStateSetProvider.SetLastIrreversibleBlockStateSetAsync(chainId, libHash);
        }
        
        await _appBlockStateSetProvider.SaveDataAsync(chainId);
        
        if (libHeight != 0)
        {
            await LocalEventBus.PublishAsync(new NewIrreversibleBlockFoundEventData
            {
                ChainId = chainId,
                BlockHash = libHash,
                BlockHeight = libHeight
            });
        }
    }

    private async Task ProcessUnfinishedLongestChainAsync(string chainId, BlockStateSet longestChainBlockStateSet)
    {
        var blockStateSets = await _appBlockStateSetProvider.GetBlockStateSetsAsync(chainId);
        var longestChain = GetLongestChain(blockStateSets, longestChainBlockStateSet.Block.BlockHash);
        if (longestChain.Count > 0)
        {
            await ProcessLongestChainAsync(chainId, longestChain, blockStateSets);
        }
    }

    private async Task ProcessLongestChainAsync(string chainId, List<BlockStateSet> longestChain, 
        Dictionary<string, BlockStateSet> blockStateSets)
    {
        var bestChainBlockStateSet = await _appBlockStateSetProvider.GetBestChainBlockStateSetAsync(chainId);
        var forkBlockStateSets = GetForkBlockStateSets(blockStateSets, longestChain, bestChainBlockStateSet?.Block.BlockHash);
        
        foreach (var blockStateSet in forkBlockStateSets)
        {
            await _fullBlockProcessor.ProcessAsync(blockStateSet.Block, true);
            await SetBlockStateSetProcessedAsync(chainId, blockStateSet, false);
        }
        
        foreach (var blockStateSet in longestChain)
        {
            await _fullBlockProcessor.ProcessAsync(blockStateSet.Block, false);
            await SetBlockStateSetProcessedAsync(chainId, blockStateSet, true);
        }

        var longestChainBlockStateSet = longestChain.LastOrDefault();
        if (longestChainBlockStateSet != null)
        {
            await _appBlockStateSetProvider.SetBestChainBlockStateSetAsync(chainId, longestChainBlockStateSet.Block.BlockHash);
        }

        await _appDataIndexManagerProvider.SavaDataAsync();
    }

    private async Task DealWithConfirmBlockAsync(BlockWithTransactionDto blockDto, BlockStateSet blockStateSet)
    {
        //Deal with confirmed block
        foreach (var change in blockStateSet.Changes)
        {
            var value = await _appStateProvider.GetLastIrreversibleStateAsync<IndexerEntity>(blockDto.ChainId,change.Key);
            if (value != null && value.Metadata.Block.BlockHeight > blockDto.BlockHeight)
            {
                continue;
            }

            await _appStateProvider.SetLastIrreversibleStateAsync(blockDto.ChainId,change.Key, change.Value);
        }
    }

    private List<BlockStateSet> GetLongestChain(Dictionary<string,BlockStateSet> blockStateSets, string blockHash)
    {
        var longestChain = new List<BlockStateSet>();

        BlockStateSet blockStateSet;
        while (blockStateSets.TryGetValue(blockHash,out blockStateSet) && !blockStateSet.Processed)
        {
            longestChain.Add(blockStateSet);
            blockHash = blockStateSet.Block.PreviousBlockHash;
        }

        if (blockStateSet == null && blockHash != "0000000000000000000000000000000000000000000000000000000000000000")
        {
            Logger.LogWarning($"Invalid block hash:{blockHash}");
            return new List<BlockStateSet>();
        }

        return longestChain.OrderBy(b => b.Block.BlockHeight).ToList();
    }

    private List<BlockStateSet> GetForkBlockStateSets(Dictionary<string, BlockStateSet> blockStateSets,
        List<BlockStateSet> longestChain, string blockHash)
    {
        var forkBlockSateSets = new List<BlockStateSet>();
        if (blockHash == null || longestChain.Count == 0)
        {
            return forkBlockSateSets;
        }

        var longestChainPreviousBlockHashes = new HashSet<string>();
        foreach (var l in longestChain)
        {
            longestChainPreviousBlockHashes.Add(l.Block.PreviousBlockHash);
        }
        while (!longestChainPreviousBlockHashes.Contains(blockHash) &&
               blockStateSets.TryGetValue(blockHash, out var blockStateSet))
        {
            forkBlockSateSets.Add(blockStateSet);
            blockHash = blockStateSet.Block.PreviousBlockHash;
        }
        return forkBlockSateSets.OrderBy(b => b.Block.BlockHeight).ToList();
    }

    private async Task SetBlockStateSetProcessedAsync(string chainId, BlockStateSet blockStateSet, bool processed)
    {
        blockStateSet.Processed = processed;
        await _appBlockStateSetProvider.UpdateBlockStateSetAsync(chainId, blockStateSet);
    }
}