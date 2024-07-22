using System.Collections.Concurrent;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockStates;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockState;

public class AppBlockStateSetProvider : IAppBlockStateSetProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, BlockStateSet>> _blockStateSets = new();
    private readonly ConcurrentDictionary<string, BlockStateSet> _longestChainBlockStateSets = new();
    private readonly ConcurrentDictionary<string, BlockStateSet> _bestChainBlockStateSets= new();
    private readonly ConcurrentDictionary<string, BlockStateSet> _lastIrreversibleBlockStateSets= new();
    
    private readonly IClusterClient _clusterClient;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly ILogger<AppBlockStateSetProvider> _logger;

    public AppBlockStateSetProvider(IClusterClient clusterClient, ILogger<AppBlockStateSetProvider> logger, IAppInfoProvider appInfoProvider)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _appInfoProvider = appInfoProvider;
    }
    
    public Task AddBlockStateSetAsync(string chainId, BlockStateSet blockStateSet)
    {
        if (!_blockStateSets.TryGetValue(chainId, out var sets))
        {
            sets = new ConcurrentDictionary<string, BlockStateSet>();
            _blockStateSets[chainId] = sets;
        }

        sets[blockStateSet.Block.BlockHash] = blockStateSet;
        
        return Task.CompletedTask;
    }
    
    public Task UpdateBlockStateSetAsync(string chainId, BlockStateSet blockStateSet)
    {
        if (!_blockStateSets.TryGetValue(chainId, out var sets) || !sets.ContainsKey(blockStateSet.Block.BlockHash))
        {
            return Task.CompletedTask;
        }
        sets[blockStateSet.Block.BlockHash] = blockStateSet;
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
            sets.RemoveAll(set =>
                set.Value.Block.BlockHeight < blockHeight || 
                (set.Value.Block.BlockHeight == blockHeight && set.Value.Block.BlockHash != blockHash));
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
        _logger.LogTrace("[{ChainId}] Saving BlockStateSetsStatus.", chainId);

        _bestChainBlockStateSets.TryGetValue(chainId, out var bestChainBlockStateSet);
        _longestChainBlockStateSets.TryGetValue(chainId, out var longestChainBlockStateSet);
        _lastIrreversibleBlockStateSets.TryGetValue(chainId, out var lastIrreversibleBlockStateSet);
        
        var appBlockStateSetsStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(GetBlockStateSetStatusKey(chainId));
        await appBlockStateSetsStatusGrain.SetBlockStateSetStatusAsync(new BlockStateSetStatus
        {
            BestChainBlockHash = bestChainBlockStateSet?.Block.BlockHash,
            BestChainHeight = bestChainBlockStateSet?.Block.BlockHeight ?? 0,
            LongestChainBlockHash = longestChainBlockStateSet?.Block.BlockHash,
            LongestChainHeight = longestChainBlockStateSet?.Block.BlockHeight ?? 0,
            LastIrreversibleBlockHash = lastIrreversibleBlockStateSet?.Block.BlockHash,
            LastIrreversibleBlockHeight = lastIrreversibleBlockStateSet?.Block.BlockHeight ?? 0,
            Branches = new Dictionary<string, long>()
        });
        
        _logger.LogTrace("[{ChainId}] Saved BlockStateSetsStatus.", chainId);
    }
    
    private string GetBlockStateSetStatusKey(string chainId)
    {
        return GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version, chainId);
    }
}