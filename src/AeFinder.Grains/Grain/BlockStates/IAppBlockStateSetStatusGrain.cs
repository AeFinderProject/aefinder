using Orleans;

namespace AeFinder.Grains.Grain.BlockStates;

public interface IAppBlockStateSetStatusGrain : IGrainWithStringKey
{
    Task<BlockStateSetStatus> GetBlockStateSetStatusAsync();
    Task SetBlockStateSetStatusAsync(BlockStateSetStatus status);
}