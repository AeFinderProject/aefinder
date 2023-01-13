using System.Collections.Concurrent;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

internal class BlockStateSetProvider<T> : IBlockStateSetProvider<T>, ISingletonDependency
{
    private ConcurrentDictionary<string, Dictionary<string, BlockStateSet<T>>> _blockStateSets = new();
    private ConcurrentDictionary<string, BlockStateSet<T>> _longestChainBlockStateSets = new();
    private ConcurrentDictionary<string, BlockStateSet<T>> _bestChainBlockStateSets= new();
    private ConcurrentDictionary<string, BlockStateSet<T>> _currentBlockStateSets = new();
    private ConcurrentDictionary<string, Dictionary<string, string>> _longestChainHashes = new();
    
    private readonly IClusterClient _clusterClient;

    public BlockStateSetProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSets(string key)
    {
        _blockStateSets.TryGetValue(key, out var value);
        return value;
    }

    public async Task<Dictionary<string, string>> GetLongestChainHashes(string key)
    {
        _longestChainHashes.TryGetValue(key, out var value);
        return value;
    }

    public async Task SetLongestChainHashes(string key, Dictionary<string, string> longestChainHashes)
    {
        _longestChainHashes[key] = longestChainHashes;
        foreach (var (blockHash,_) in _longestChainHashes[key])
        {
            _blockStateSets[key][blockHash].Changes = new();
        }
    }

    public async Task AddBlockStateSet(string key, BlockStateSet<T> blockStateSet)
    {
        if (blockStateSet == null)
        {
            return;
        }

        if (!_blockStateSets.TryGetValue(key, out var sets))
        {
            sets = new Dictionary<string, BlockStateSet<T>>();
        }

        sets.TryAdd(blockStateSet.BlockHash, blockStateSet);
        _blockStateSets[key] = sets;
    }

    public async Task SetBlockStateSet(string key, BlockStateSet<T> blockStateSet)
    {
        if (blockStateSet == null)
        {
            return;
        }

        if (!_blockStateSets.TryGetValue(key, out var sets))
        {
            sets = new Dictionary<string, BlockStateSet<T>>();
        }

        sets[blockStateSet.BlockHash] = blockStateSet;
        _blockStateSets[key] = sets;
    }

    public async Task<BlockStateSet<T>> GetCurrentBlockStateSet(string key)
    {
        _currentBlockStateSets.TryGetValue(key, out var value);
        return value;
    }

    public async Task<BlockStateSet<T>> GetLongestChainBlockStateSet(string key)
    {
        _longestChainBlockStateSets.TryGetValue(key, out var value);
        return value;
    }

    public async Task<BlockStateSet<T>> GetBestChainBlockStateSet(string key)
    {
        _bestChainBlockStateSets.TryGetValue(key, out var value);
        return value;
    }

    public async Task SetBestChainBlockStateSet(string key, string blockHash)
    {
        if (_blockStateSets.TryGetValue(key, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            _bestChainBlockStateSets[key] = set;
        }
    }

    public async Task SetLongestChainBlockStateSet(string key, string blockHash)
    {
        if (_blockStateSets.TryGetValue(key, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            _longestChainBlockStateSets[key] = set;
        }
    }

    public async Task SetBlockStateSetProcessed(string key, string blockHash)
    {
        if (_blockStateSets.TryGetValue(key, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            set.Processed = true;
        }
    }

    public async Task SetCurrentBlockStateSet(string key, BlockStateSet<T> blockStateSet)
    {
        _currentBlockStateSets[key] = blockStateSet;
    }

    public async Task CleanBlockStateSets(string key, long blockHeight, string blockHash)
    {
        if (_blockStateSets.TryGetValue(key, out var sets))
        {
            sets.RemoveAll(set => set.Value.BlockHeight < blockHeight);
            sets.RemoveAll(set => set.Value.BlockHeight == blockHeight && set.Value.BlockHash != blockHash);
        }
    }

    public async Task CommitAsync(string key)
    {
        var blockStateSetsGrain = _clusterClient.GetGrain<IBlockStateSetsGrain<T>>(key);
        // TODO：Set all blocksets
        //await blockStateSetsGrain.SetBlockStateSet()
        await blockStateSetsGrain.SetLongestChainBlockStateSet(_longestChainBlockStateSets[key].BlockHash);
        await blockStateSetsGrain.SetBestChainBlockStateSet(_bestChainBlockStateSets[key].BlockHash);
    }
}