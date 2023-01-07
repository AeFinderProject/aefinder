using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public class BlockStateSetsGrain<T> : Grain<BlockStateSetsGrainState<T>>, IBlockStateSetsGrain<T>
{
    public Task<Dictionary<string, string>> GetLongestChainHashes()
    {
        return Task.FromResult(State.LongestCainHashes);
    }

    public async Task SetLongestChainHashes(Dictionary<string, string> longestCainHashes,bool isChangesReset = false)
    {
        State.LongestCainHashes = longestCainHashes;
        if (isChangesReset)
        {
            foreach (var (blockHash,_) in longestCainHashes)
            {
                State.BlockStateSets[blockHash].Changes = new();
            }
        }
        await WriteStateAsync();
    }

    public async Task AddBlockStateSet(BlockStateSet<T> blockStateSet)
    {
        if (blockStateSet == null) return;

        State.BlockStateSets.TryAdd(blockStateSet.BlockHash, blockStateSet);
        await WriteStateAsync();
    }

    public async Task SetBlockStateSet(BlockStateSet<T> blockStateSet)
    {
        if (blockStateSet == null) return;
        State.BlockStateSets[blockStateSet.BlockHash] = blockStateSet;
        await WriteStateAsync();
    }
    
    public Task<BlockStateSet<T>> GetCurrentBlockStateSet()
    {
        return Task.FromResult(State.CurrentBlockStateSet);
    }

    public Task<BlockStateSet<T>> GetLongestChainBlockStateSet()
    {
        return Task.FromResult(State.LongestCainBlockStateSet);
    }

    public Task<BlockStateSet<T>> GetBestChainBlockStateSet()
    {
        return Task.FromResult(State.BestCainBlockStateSet);
    }

    public async Task SetBestChainBlockStateSet(string blockHash)
    {
        if (State.BlockStateSets.TryGetValue(blockHash, out var blockStateSet))
        {
            State.BestCainBlockStateSet = blockStateSet;
            await WriteStateAsync();
        }
    }

    public async Task SetBlockStateSetProcessed(string blockHash)
    {
        if (State.BlockStateSets.TryGetValue(blockHash, out _))
        {
            State.BlockStateSets[blockHash].Processed = true;
            await WriteStateAsync();
        }
    }

    public async Task SetLongestChainBlockStateSet(string blockHash)
    {
        if (State.BlockStateSets.TryGetValue(blockHash, out var blockStateSet))
        {
            State.LongestCainBlockStateSet = blockStateSet;
            await WriteStateAsync();
        }
    }

    public async Task SetCurrentBlockStateSet(BlockStateSet<T> blockStateSet)
    {
        State.CurrentBlockStateSet = blockStateSet;
        await WriteStateAsync();
    }

    public Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSets()
    {
        return Task.FromResult(State.BlockStateSets);
    }

    // public Task<bool> TryGetBlockStateSet(string blockHash, out BlockStateSet<T> blockStateSet)
    // {
    //     return Task.FromResult(State.BlockStateSets.TryGetValue(blockHash, out blockStateSet));
    // }

    public async Task CleanBlockStateSets(long blockHeight, string blockHash)
    {
        State.BlockStateSets.RemoveAll(set => set.Value.BlockHeight < blockHeight);
        State.BlockStateSets.RemoveAll(set => set.Value.BlockHeight == blockHeight && set.Value.BlockHash != blockHash);
        // if (State.HasFork)
        // {
        //     State.HasFork = State.BlockStateSets.GroupBy(b => b.Value.BlockHeight).Any(g => g.Count() > 1);
        // }

        await WriteStateAsync();
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }
}