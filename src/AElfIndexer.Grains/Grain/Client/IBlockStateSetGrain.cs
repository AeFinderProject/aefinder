using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public interface IBlockStateSetGrain : IGrainWithStringKey
{
    Task SetBlockStateSetsAsync(Dictionary<string, AppBlockStateSet> sets);
    Task<Dictionary<string, AppBlockStateSet>> GetBlockStateSetsAsync();
    Task SetLongestChainBlockHashAsync(string blockHash);
    Task<AppBlockStateSet> GetLongestChainBlockStateSetAsync();
    Task SetBestChainBlockHashAsync(string blockHash);
    Task<AppBlockStateSet> GetBestChainBlockStateSetAsync();
}