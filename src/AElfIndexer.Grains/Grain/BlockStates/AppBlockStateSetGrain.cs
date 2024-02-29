using AElfIndexer.Grains.State.BlockStates;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockStates;

public class AppBlockStateSetGrain : Grain<AppBlockStateSetState>, IAppBlockStateSetGrain
{
    public async Task<BlockStateSet> GetBlockStateSetAsync()
    {
        await ReadStateAsync();
        return State.BlockStateSet;
    }

    public async Task SetBlockStateSetAsync(BlockStateSet set)
    {
        State.BlockStateSet= set;
        await WriteStateAsync();
    }

    public async Task RemoveBlockStateSetAsync()
    {
        await ClearStateAsync();
        DeactivateOnIdle();
    }
}