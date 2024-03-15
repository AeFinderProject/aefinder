using Orleans;

namespace AeFinder.Grains.Grain.BlockStates;

public interface IAppBlockStateSetGrain : IGrainWithStringKey
{
    Task<BlockStateSet> GetBlockStateSetAsync();
    Task SetBlockStateSetAsync(BlockStateSet set);
    Task RemoveBlockStateSetAsync();
}