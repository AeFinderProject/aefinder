using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public class BlockStateSetBucketGrain<T> : Grain<BlockStateSetBucketState<T>>, IBlockStateSetBucketGrain<T>
{
    public async Task SetBlockStateSetsAsync(Dictionary<string, BlockStateSet<T>> sets)
    {
        State.BlockStateSets = sets;
        await WriteStateAsync();
    }

    public Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync()
    {
        return Task.FromResult(State.BlockStateSets);
    }
    public Task<BlockStateSet<T>> GetBlockStateSetAsync(string blockHash)
    {
        return Task.FromResult(State.BlockStateSets.TryGetValue(blockHash, out var set) ? set : null);
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }
}