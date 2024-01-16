using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public interface IBlockStateSetBucketGrain : IGrainWithStringKey
{
    Task SetBlockStateSetsAsync(string version, Dictionary<string, AppBlockStateSet> sets);
    Task<Dictionary<string, AppBlockStateSet>> GetBlockStateSetsAsync(string version);
    Task<AppBlockStateSet> GetBlockStateSetAsync(string version, string blockHash);
    Task CleanAsync(string version);
}