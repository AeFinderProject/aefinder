using AeFinder.Grains.State.BlockStates;
using Orleans;

namespace AeFinder.Grains.Grain.BlockStates;

public interface IAppBlockStateChangeGrain : IGrainWithStringKey
{
    Task<Dictionary<string, BlockStateChange>> GetAsync();
    Task SetAsync(long blockHeight, Dictionary<string, BlockStateChange> changes);
    Task RemoveAsync();
}