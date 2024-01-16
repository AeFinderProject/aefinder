using AeFinder.Grains.State.Client;
using Orleans;

namespace AeFinder.Grains.Grain.Client;

public interface IBlockStateSetGrain<T> : IGrainWithStringKey
{
    Task SetBlockStateSetsAsync(Dictionary<string, BlockStateSet<T>> sets);
    Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync();
    Task SetLongestChainBlockHashAsync(string blockHash);
    Task<BlockStateSet<T>> GetLongestChainBlockStateSetAsync();
    Task SetBestChainBlockHashAsync(string blockHash);
    Task<BlockStateSet<T>> GetBestChainBlockStateSetAsync();
}