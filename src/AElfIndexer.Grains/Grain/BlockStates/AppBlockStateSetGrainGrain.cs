using AElfIndexer.Grains.State.BlockState;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockStates;

public class AppBlockStateSetGrainGrain : Grain<AppBlockStateSetState>, IAppBlockStateSetGrain
{
    public async Task<AppBlockStateSet> GetBlockStateSetAsync()
    {
        await ReadStateAsync();
        return State.BlockStateSet;
    }

    public async Task SetBlockStateSetAsync(AppBlockStateSet set)
    {
        State.BlockStateSet= set;
        await WriteStateAsync();
    }

    public async Task RemoveBlockStateSetAsync()
    {
        await ClearStateAsync();
    }
}