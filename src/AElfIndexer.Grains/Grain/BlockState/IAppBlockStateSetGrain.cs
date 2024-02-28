using Orleans;

namespace AElfIndexer.Grains.Grain.BlockState;

public interface IAppBlockStateSetGrain : IGrainWithStringKey
{
    Task<BlockStateSet> GetBlockStateSetAsync();
    Task SetBlockStateSetAsync(BlockStateSet set);
    Task RemoveBlockStateSetAsync();
}