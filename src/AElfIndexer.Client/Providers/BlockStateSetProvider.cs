using System.Collections.Concurrent;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

internal class BlockStateSetProvider<T> : IBlockStateSetProvider<T>, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, Dictionary<string, BlockStateSet<T>>> _blockStateSets = new();
    private readonly ConcurrentDictionary<string, BlockStateSet<T>> _longestChainBlockStateSets = new();
    private readonly ConcurrentDictionary<string, BlockStateSet<T>> _bestChainBlockStateSets= new();
    private readonly ConcurrentDictionary<string, BlockStateSet<T>> _currentBlockStateSets = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _longestChainHashes = new();
    
    private readonly IClusterClient _clusterClient;

    public BlockStateSetProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync(string key)
    {
        if (!_blockStateSets.TryGetValue(key, out var value))
        { 
            value = new Dictionary<string, BlockStateSet<T>>();
            _blockStateSets[key] = value;
        }

        return Task.FromResult(value);
    }

    public Task<Dictionary<string, string>> GetLongestChainHashesAsync(string key)
    {
        _longestChainHashes.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetLongestChainHashesAsync(string key, Dictionary<string, string> longestChainHashes)
    {
        _longestChainHashes[key] = longestChainHashes;
        foreach (var (blockHash,_) in _longestChainHashes[key])
        {
            _blockStateSets[key][blockHash].Changes = new();
        }

        return Task.CompletedTask;
    }

    public Task AddBlockStateSetAsync(string key, BlockStateSet<T> blockStateSet)
    {
        if (blockStateSet == null)
        {
            return Task.CompletedTask;
        }

        if (!_blockStateSets.TryGetValue(key, out var sets))
        {
            sets = new Dictionary<string, BlockStateSet<T>>();
        }

        sets.TryAdd(blockStateSet.BlockHash, blockStateSet);
        _blockStateSets[key] = sets;
        return Task.CompletedTask;
    }

    public Task SetBlockStateSetAsync(string key, BlockStateSet<T> blockStateSet)
    {
        if (blockStateSet == null)
        {
            return Task.CompletedTask;
        }

        if (!_blockStateSets.TryGetValue(key, out var sets))
        {
            sets = new Dictionary<string, BlockStateSet<T>>();
        }

        sets[blockStateSet.BlockHash] = blockStateSet;
        _blockStateSets[key] = sets;
        return Task.CompletedTask;
    }

    public Task<BlockStateSet<T>> GetCurrentBlockStateSetAsync(string key)
    {
        _currentBlockStateSets.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task<BlockStateSet<T>> GetLongestChainBlockStateSetAsync(string key)
    {
        _longestChainBlockStateSets.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task<BlockStateSet<T>> GetBestChainBlockStateSetAsync(string key)
    {
        _bestChainBlockStateSets.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    public Task SetBestChainBlockStateSetAsync(string key, string blockHash)
    {
        if (_blockStateSets.TryGetValue(key, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            _bestChainBlockStateSets[key] = set;
        }

        return Task.CompletedTask;
    }

    public Task SetLongestChainBlockStateSetAsync(string key, string blockHash)
    {
        if (_blockStateSets.TryGetValue(key, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            _longestChainBlockStateSets[key] = set;
        }

        return Task.CompletedTask;
    }

    public Task SetBlockStateSetProcessedAsync(string key, string blockHash, bool processed)
    {
        if (!_blockStateSets.TryGetValue(key, out var sets) || !sets.TryGetValue(blockHash, out var set))
        {
            return Task.CompletedTask;
        }

        set.Processed = processed;

        return Task.CompletedTask;
    }

    public Task SetCurrentBlockStateSetAsync(string key, BlockStateSet<T> blockStateSet)
    {
        _currentBlockStateSets[key] = blockStateSet;
        return Task.CompletedTask;
    }

    public Task CleanBlockStateSetsAsync(string key, long blockHeight, string blockHash)
    {
        if (_blockStateSets.TryGetValue(key, out var sets))
        {
            sets.RemoveAll(set => set.Value.BlockHeight < blockHeight);
            sets.RemoveAll(set => set.Value.BlockHeight == blockHeight && set.Value.BlockHash != blockHash);
        }

        return Task.CompletedTask;
    }

    public async Task SaveDataAsync(string key)
    {
        var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetsGrain<T>>(key);
        await blockStateSetsGrain.SetBlockStateSets(_blockStateSets[key]);
        await blockStateSetsGrain.SetLongestChainBlockStateSet(_longestChainBlockStateSets[key].BlockHash);
        await blockStateSetsGrain.SetBestChainBlockStateSet(_bestChainBlockStateSets[key].BlockHash);
    }
}