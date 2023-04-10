using System.Collections.Concurrent;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

public class BlockStateSetProvider<T> : IBlockStateSetProvider<T>, ISingletonDependency
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

    public async Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync(string key)
    {
        if (!_blockStateSets.TryGetValue(key, out var value))
        { 
            var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetGrain<T>>(key);
            value = await blockStateSetsGrain.GetBlockStateSetsAsync();
            _blockStateSets[key] = value;
        }

        return value;
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
    
    public Task SetBlockStateSetAsync(string key, BlockStateSet<T> blockStateSet)
    {

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

    public async Task<BlockStateSet<T>> GetLongestChainBlockStateSetAsync(string key)
    {
        if (!_longestChainBlockStateSets.TryGetValue(key, out var value))
        {
            var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetGrain<T>>(key);
            value = await blockStateSetsGrain.GetLongestChainBlockStateSetAsync();
            _longestChainBlockStateSets[key] = value;
        }
        
        return value;
    }

    public async Task<BlockStateSet<T>> GetBestChainBlockStateSetAsync(string key)
    {
        if (!_bestChainBlockStateSets.TryGetValue(key, out var value))
        {
            var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetGrain<T>>(key);
            value = await blockStateSetsGrain.GetBestChainBlockStateSetAsync();
            _bestChainBlockStateSets[key] = value;
        }

        return value;
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
        var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetGrain<T>>(key);
        await blockStateSetsGrain.SetBlockStateSetsAsync(_blockStateSets[key]);
        if (_longestChainBlockStateSets.TryGetValue(key, out var longestChainSets) && longestChainSets != null)
        {
            await blockStateSetsGrain.SetLongestChainBlockHashAsync(longestChainSets.BlockHash);
        }

        if (_bestChainBlockStateSets.TryGetValue(key, out var bestChainSets) && bestChainSets != null)
        {
            await blockStateSetsGrain.SetBestChainBlockHashAsync(bestChainSets.BlockHash);
        }
    }
}