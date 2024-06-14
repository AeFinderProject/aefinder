using AeFinder.Grains.State.BlockStates;
using Orleans;

namespace AeFinder.Grains.Grain.BlockStates;

public interface IAppBlockStateChangeGrain : IGrainWithStringKey
{
    Task<BlockStateChange> GetAsync();
    Task SetAsync(BlockStateChange change);
    Task RemoveAsync();
}