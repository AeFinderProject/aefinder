using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public interface IBlockStateSetBucketGrain<T> : IGrainWithStringKey
{
    Task SetBlockStateSetsAsync(Dictionary<string, BlockStateSet<T>> sets);
    Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync();
    Task<BlockStateSet<T>> GetBlockStateSetAsync(string blockHash);
}