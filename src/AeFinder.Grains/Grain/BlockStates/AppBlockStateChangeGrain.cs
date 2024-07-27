using AeFinder.Grains.State.BlockStates;
using Orleans;

namespace AeFinder.Grains.Grain.BlockStates;

public class AppBlockStateChangeGrain : AeFinderGrain<AppBlockStateChangeState>, IAppBlockStateChangeGrain
{
    public async Task<Dictionary<string, BlockStateChange>> GetAsync()
    {
        await ReadStateAsync();
        return State.Changes;
    }

    public async Task SetAsync(long blockHeight, Dictionary<string, BlockStateChange> changes)
    {
        State.BlockHeight = blockHeight;
        State.Changes = changes;
        await WriteStateAsync();
    }

    public async Task RemoveAsync()
    {
        await ClearStateAsync();
        DeactivateOnIdle();
    }
}