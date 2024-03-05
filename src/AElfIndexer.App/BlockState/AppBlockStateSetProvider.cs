using System.Collections.Concurrent;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.BlockStates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.App.BlockState;

public class AppBlockStateSetProvider : IAppBlockStateSetProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BlockStateSet>> _blockStateSets = new();
    private readonly ConcurrentDictionary<string, BlockStateSet> _longestChainBlockStateSets = new();
    private readonly ConcurrentDictionary<string, BlockStateSet> _bestChainBlockStateSets= new();
    private readonly ConcurrentDictionary<string, BlockStateSet> _lastIrreversibleBlockStateSets= new();
    private readonly ConcurrentDictionary<string, Dictionary<string, long>> _branches = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, ChangedBlockStateSet>> _changedBlockStateSets = new();
    
    private readonly IClusterClient _clusterClient;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly ILogger<AppBlockStateSetProvider> _logger;

    public AppBlockStateSetProvider(IClusterClient clusterClient, ILogger<AppBlockStateSetProvider> logger, IAppInfoProvider appInfoProvider)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _appInfoProvider = appInfoProvider;
    }

    public async Task InitializeAsync(string chainId)
    {
        if (_blockStateSets.ContainsKey(chainId))
        {
            return;
        }

        var blockStateSets = new ConcurrentDictionary<string, BlockStateSet>();
        _blockStateSets[chainId] = blockStateSets;
        
        var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(GetBlockStateSetStatusKey(chainId));
        var status = await appBlockStateSetStatusGrain.GetBlockStateSetStatusAsync();
        var tasks = status.Branches.Select(o => InitializeBranchBlockStateSetsAsync(chainId, o.Key, blockStateSets));
        await tasks.WhenAll();

        if (blockStateSets.Count == 0)
        {
            return;
        }

        if (!status.LongestChainBlockHash.IsNullOrWhiteSpace())
        {
            _longestChainBlockStateSets[chainId] = blockStateSets[status.LongestChainBlockHash];
        }

        if (!status.BestChainBlockHash.IsNullOrWhiteSpace())
        {
            _bestChainBlockStateSets[chainId] = blockStateSets[status.BestChainBlockHash];
        }
        
        if (!status.LastIrreversibleBlockHash.IsNullOrWhiteSpace())
        {
            _lastIrreversibleBlockStateSets[chainId] = blockStateSets[status.LastIrreversibleBlockHash];
        }

        _branches[chainId] = status.Branches;
    }
    
    private async Task InitializeBranchBlockStateSetsAsync(string chainId, string blockHash, ConcurrentDictionary<string,BlockStateSet> blockStateSets)
    {
        var appBlockStateSetGrain = _clusterClient.GetGrain<IAppBlockStateSetGrain>(GetBlockStateSetKey(chainId, blockHash));
        var set = await appBlockStateSetGrain.GetBlockStateSetAsync();
        while (set!= null)
        {
            blockStateSets[set.Block.BlockHash] = set;
            
            appBlockStateSetGrain = _clusterClient.GetGrain<IAppBlockStateSetGrain>(GetBlockStateSetKey(chainId, set.Block.PreviousBlockHash));
            set = await appBlockStateSetGrain.GetBlockStateSetAsync();
        }
    }

    public Task AddBlockStateSetAsync(string chainId, BlockStateSet blockStateSet)
    {
        if (!_blockStateSets.TryGetValue(chainId, out var sets))
        {
            sets = new ConcurrentDictionary<string, BlockStateSet>();
            _blockStateSets[chainId] = sets;
        }

        sets[blockStateSet.Block.BlockHash] = blockStateSet;

        UpdateBranch(chainId,blockStateSet);
        AddChangedBlockStateSet(chainId, blockStateSet, DataOperationType.AddOrUpdate);
        
        return Task.CompletedTask;
    }
    
    public Task UpdateBlockStateSetAsync(string chainId, BlockStateSet blockStateSet)
    {
        if (!_blockStateSets.TryGetValue(chainId, out var sets) || !sets.ContainsKey(blockStateSet.Block.BlockHash))
        {
            return Task.CompletedTask;
        }
        sets[blockStateSet.Block.BlockHash] = blockStateSet;
        AddChangedBlockStateSet(chainId, blockStateSet, DataOperationType.AddOrUpdate);
        return Task.CompletedTask;
    }

    public Task<BlockStateSet> GetLongestChainBlockStateSetAsync(string chainId)
    {
        _longestChainBlockStateSets.TryGetValue(chainId, out var value);
        return Task.FromResult(value);
    }

    public Task<BlockStateSet> GetBestChainBlockStateSetAsync(string chainId)
    {
        _bestChainBlockStateSets.TryGetValue(chainId, out var value);
        return Task.FromResult(value);
    }

    public Task<BlockStateSet> GetLastIrreversibleBlockStateSetAsync(string chainId)
    {
        _lastIrreversibleBlockStateSets.TryGetValue(chainId, out var value);
        return Task.FromResult(value);
    }

    public Task SetBestChainBlockStateSetAsync(string chainId, string blockHash)
    {
        if (_blockStateSets.TryGetValue(chainId, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            _bestChainBlockStateSets[chainId] = set;
        }

        return Task.CompletedTask;
    }

    public Task SetLongestChainBlockStateSetAsync(string chainId, string blockHash)
    {
        if (_blockStateSets.TryGetValue(chainId, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            _longestChainBlockStateSets[chainId] = set;
        }

        return Task.CompletedTask;
    }

    public Task SetLastIrreversibleBlockStateSetAsync(string chainId, string blockHash)
    {
        if (_blockStateSets.TryGetValue(chainId, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            _lastIrreversibleBlockStateSets[chainId] = set;
        }

        return Task.CompletedTask;
    }

    public void CleanBlockStateSets(string chainId, long blockHeight, string blockHash)
    {
        if (_blockStateSets.TryGetValue(chainId, out var sets))
        {
            var toRemovedSets = sets.RemoveAll(set => set.Value.Block.BlockHeight < blockHeight).ToList();
            toRemovedSets.AddRange(sets.RemoveAll(set => set.Value.Block.BlockHeight == blockHeight && set.Value.Block.BlockHash != blockHash));

            foreach (var item in toRemovedSets)
            {
                RemoveBranch(chainId,item.Value);
                AddChangedBlockStateSet(chainId, item.Value, DataOperationType.Delete);
            }
        }
    }
    
    public Task<BlockStateSet> GetBlockStateSetAsync(string chainId, string blockHash)
    {
        if (_blockStateSets.TryGetValue(chainId, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            return Task.FromResult(set);
        }

        return Task.FromResult<BlockStateSet>(null);
    }
    
    public Task<int> GetBlockStateSetCountAsync(string chainId)
    {
        if (_blockStateSets.TryGetValue(chainId, out var sets))
        {
            return Task.FromResult(sets.Count);
        }

        return Task.FromResult<int>(0);
    }

    public async Task SaveDataAsync(string chainId)
    {
        _logger.LogDebug("Saving BlockStateSets. ChainId: {ChainId}.", chainId);

        if (_changedBlockStateSets.TryGetValue(chainId, out var changedBlockStateSet) && changedBlockStateSet.Count > 0)
        {
            _logger.LogDebug("Changed BlockStateSet Count: {Count}", changedBlockStateSet.Count);
            var tasks = changedBlockStateSet.Select(o =>
            {
                var appBlockStateSetsGrain =
                    _clusterClient.GetGrain<IAppBlockStateSetGrain>(GetBlockStateSetKey(chainId,
                        o.Value.BlockStateSet.Block.BlockHash));
                switch (o.Value.OperationType)
                {
                    case DataOperationType.AddOrUpdate:
                        return appBlockStateSetsGrain.SetBlockStateSetAsync(o.Value.BlockStateSet);
                    case DataOperationType.Delete:
                        return appBlockStateSetsGrain.RemoveBlockStateSetAsync();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });
            await Task.WhenAll(tasks);
        }

        _bestChainBlockStateSets.TryGetValue(chainId, out var bestChainBlockStateSet);
        _longestChainBlockStateSets.TryGetValue(chainId, out var longestChainBlockStateSet);
        _lastIrreversibleBlockStateSets.TryGetValue(chainId, out var lastIrreversibleBlockStateSet);
        var branches = _branches.TryGetValue(chainId, out var branchHashes) ? branchHashes : new Dictionary<string, long>();
        
        var appBlockStateSetsStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(GetBlockStateSetStatusKey(chainId));
        await appBlockStateSetsStatusGrain.SetBlockStateSetStatusAsync(new BlockStateSetStatus
        {
            BestChainBlockHash = bestChainBlockStateSet?.Block.BlockHash,
            BestChainHeight = bestChainBlockStateSet?.Block.BlockHeight ?? 0,
            LongestChainBlockHash = longestChainBlockStateSet?.Block.BlockHash,
            LongestChainHeight = longestChainBlockStateSet?.Block.BlockHeight ?? 0,
            LastIrreversibleBlockHash = lastIrreversibleBlockStateSet?.Block.BlockHash,
            LastIrreversibleBlockHeight = lastIrreversibleBlockStateSet?.Block.BlockHeight ?? 0,
            Branches = branches
        });
        
        changedBlockStateSet?.Clear();
        
        _logger.LogDebug("Saved BlockStateSets. ChainId: {ChainId}", chainId);
    }
    
    private void AddChangedBlockStateSet(string chainId, BlockStateSet blockStateSet, DataOperationType operationType)
    {
        if (!_changedBlockStateSets.TryGetValue(chainId, out var changedSets))
        {
            changedSets = new Dictionary<string, ChangedBlockStateSet>();
        }
        changedSets[blockStateSet.Block.BlockHash] = new ChangedBlockStateSet
        {
            OperationType = operationType,
            BlockStateSet = blockStateSet
        };
        _changedBlockStateSets[chainId] = changedSets;
    }

    private void UpdateBranch(string chainId, BlockStateSet blockStateSet)
    {
        if (!_branches.TryGetValue(chainId, out var branches))
        {
            branches = new Dictionary<string, long>();
            _branches[chainId] = branches;
        }

        branches.Remove(blockStateSet.Block.PreviousBlockHash);
        branches.Add(blockStateSet.Block.BlockHash,blockStateSet.Block.BlockHeight);
    }
    
    private void RemoveBranch(string chainId, BlockStateSet blockStateSet)
    {
        if (!_branches.TryGetValue(chainId, out var branches))
        {
            return;
        }

        branches.Remove(blockStateSet.Block.BlockHash);
    }
    
    private string GetBlockStateSetKey(string chainId, string blockHash)
    {
        return GrainIdHelper.GenerateAppBlockStateSetGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
            chainId, blockHash);
    }
    
    private string GetBlockStateSetStatusKey(string chainId)
    {
        return GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version, chainId);
    }
}

public class ChangedBlockStateSet
{
    public DataOperationType OperationType { get; set; }
    public BlockStateSet BlockStateSet { get; set; }
}