using AeFinder.Grains.State.Client;
using Orleans;

namespace AeFinder.Grains.Grain.Client;

public interface IBlockStateSetBucketGrain<T> : IGrainWithStringKey
{
    Task SetBlockStateSetsAsync(string version, Dictionary<string, BlockStateSet<T>> sets);
    Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync(string version);
    Task<BlockStateSet<T>> GetBlockStateSetAsync(string version, string blockHash);
    Task CleanAsync(string version);
}