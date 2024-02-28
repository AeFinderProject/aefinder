using Orleans;

namespace AElfIndexer.Grains.Grain.BlockState;

public interface IAppBlockStateSetStatusGrain : IGrainWithStringKey
{
    Task<BlockStateSetStatus> GetBlockStateSetStatusAsync();
    Task SetBlockStateSetStatusAsync(BlockStateSetStatus status);
}