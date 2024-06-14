using AeFinder.Grains.State.BlockStates;
using Orleans;

namespace AeFinder.Grains.Grain.BlockStates;

public class AppBlockStateChangeGrain : Grain<AppBlockStateChangeState>, IAppBlockStateChangeGrain
{
    public async Task<BlockStateChange> GetAsync()
    {
        await ReadStateAsync();
        return State.BlockStateChange;
    }

    public async Task SetAsync(BlockStateChange change)
    {
        State.BlockStateChange = change;
        await WriteStateAsync();
    }

    public async Task RemoveAsync()
    {
        await ClearStateAsync();
        DeactivateOnIdle();
    }
}