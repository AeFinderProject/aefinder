using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public class BlockStateSetBucketGrain : Grain<BlockStateSetBucketState>, IBlockStateSetBucketGrain
{
    public async Task SetBlockStateSetsAsync(string version, Dictionary<string, AppBlockStateSet> sets)
    {
        State.BlockStateSets[version] = sets;
        await WriteStateAsync();
    }

    public async Task<Dictionary<string, AppBlockStateSet>> GetBlockStateSetsAsync(string version)
    {
        if (State.BlockStateSets.TryGetValue(version, out var sets))
        {
            return sets;
        }

        return new Dictionary<string, AppBlockStateSet>();
    }

    public async Task<AppBlockStateSet> GetBlockStateSetAsync(string version, string blockHash)
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