using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public interface IBlockStateSetManagerGrain<T> : IGrainWithStringKey
{
    Task SetBlockStateSetsAsync(Dictionary<string, BlockStateSet<T>> sets);
    Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync();
    Task SetLongestChainBlockHashAsync(string blockHash);
    Task<BlockStateSet<T>> GetLongestChainBlockStateSetAsync();
    Task SetBestChainBlockHashAsync(string blockHash);
    Task<BlockStateSet<T>> GetBestChainBlockStateSetAsync();
}