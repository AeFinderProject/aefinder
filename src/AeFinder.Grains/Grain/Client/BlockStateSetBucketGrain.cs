using AeFinder.Grains.State.Client;
using Orleans;

namespace AeFinder.Grains.Grain.Client;

public class BlockStateSetBucketGrain<T> : Grain<BlockStateSetBucketState<T>>, IBlockStateSetBucketGrain<T>
{
    public async Task SetBlockStateSetsAsync(string version, Dictionary<string, BlockStateSet<T>> sets)
    {
        State.BlockStateSets[version] = sets;
        await WriteStateAsync();
    }

    public async Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync(string version)
    {
        if (State.BlockStateSets.TryGetValue(version, out var sets))
        {
            return sets;
        }

        return new Dictionary<string, BlockStateSet<T>>();
    }

    public async Task<BlockStateSet<T>> GetBlockStateSetAsync(string version, string blockHash)
    {
        if (State.BlockStateSets.TryGetValue(version, out var sets) && sets.TryGetValue(blockHash, out var set))
        {
            return set;
        }

        return null;
    }

    public async Task CleanAsync(string version)
    {
        State.BlockStateSets.RemoveAll(o => o.Key != version);
        await WriteStateAsync();
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }
}