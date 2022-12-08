using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public class BlockStateSetsGrain<T> : Grain<BlockStateSetsGrainState<T>>, IBlockStateSetsGrain<T>
{
    public Task<Dictionary<string, string>> GetBestChainHashes()
    {
        return Task.FromResult(State.BestChainHashes);
    }

    public async Task SetBestChainHashes(Dictionary<string, string> bestChainHashes)
    {
        State.BestChainHashes = bestChainHashes;
        await WriteStateAsync();
    }

    public async Task<bool> TryAddBlockStateSet(BlockStateSet<T> blockStateSet)
    {
        if (blockStateSet == null)
        {
            return false;
        }

        // BlockStateSet will not be added to State.BlockStateSets 
        State.BlockStateSets.TryAdd(blockStateSet.BlockHash, blockStateSet);
        if (blockStateSet.PreviousBlockHash != State.CurrentBlockStateSet.BlockHash)
        {
            State.HasFork = true;
        }
        State.CurrentBlockStateSet = blockStateSet;
        await WriteStateAsync();
        return false;
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
    
    public Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSets()
    {
        return Task.FromResult(State.BlockStateSets);
    }

    // public Task<bool> TryGetBlockStateSet(string blockHash, out BlockStateSet<T> blockStateSet)
    // {
    //     return Task.FromResult(State.BlockStateSets.TryGetValue(blockHash, out blockStateSet));
    // }
    
    public Task<bool> HasFork()
    {
        //TODO 需要再确认下是否有问题
        return Task.FromResult(State.HasFork);
    }

    public async Task CleanBlockStateSets(long blockHeight, string blockHash)
    {
        State.BlockStateSets.RemoveAll(set => set.Value.BlockHeight < blockHeight);
        State.BlockStateSets.RemoveAll(set => set.Value.BlockHeight == blockHeight && set.Value.BlockHash != blockHash);
        if (State.HasFork)
        {
            State.HasFork = State.BlockStateSets.GroupBy(b => b.Value.BlockHeight).Any(g => g.Count() > 1);
        }

        await WriteStateAsync();
    }

    public async Task Initialize()
    {
        State.CurrentBlockStateSet = null;
        State.BlockStateSets = new();
        State.HasFork = false;
        await WriteStateAsync();
    }

    public override Task OnActivateAsync()
    {
        ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        WriteStateAsync();
        return base.OnDeactivateAsync();
    }
}