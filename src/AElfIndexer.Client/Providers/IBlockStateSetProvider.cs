using System.Collections.Concurrent;
using AElfIndexer.Grains.State.Client;

namespace AElfIndexer.Client.Providers;

public interface IBlockStateSetProvider
{
    Task<Dictionary<string, AppBlockStateSet>> GetBlockStateSetsAsync(string key);
    Task<Dictionary<string, string>> GetLongestChainHashesAsync(string key);
    Task SetLongestChainHashesAsync(string key, Dictionary<string, string> longestChainHashes);
    Task SetBlockStateSetAsync(string key, AppBlockStateSet blockStateSet);
    Task<AppBlockStateSet> GetCurrentBlockStateSetAsync(string key);
    Task<AppBlockStateSet> GetLongestChainBlockStateSetAsync(string key);
    Task<AppBlockStateSet> GetBestChainBlockStateSetAsync(string key);
    Task SetBestChainBlockStateSetAsync(string key, string blockHash);
    Task SetLongestChainBlockStateSetAsync(string key, string blockHash);
    Task SetBlockStateSetProcessedAsync(string key, string blockHash, bool processed);
    Task SetCurrentBlockStateSetAsync(string key, AppBlockStateSet blockStateSet);
    Task CleanBlockStateSetsAsync(string key, long blockHeight,string blockHash);
    Task SaveDataAsync(string key);
}