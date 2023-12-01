using System.Collections.Concurrent;
using AElfIndexer.Grains.State.Client;

namespace AElfIndexer.Client.Providers;

public interface IBlockStateSetProvider<T>
{
    Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSetsAsync(string key);
    Task<Dictionary<string, string>> GetLongestChainHashesAsync(string key);
    Task SetLongestChainHashesAsync(string key, Dictionary<string, string> longestChainHashes);
    Task SetBlockStateSetAsync(string key, BlockStateSet<T> blockStateSet);
    Task<BlockStateSet<T>> GetCurrentBlockStateSetAsync(string key);
    Task<BlockStateSet<T>> GetLongestChainBlockStateSetAsync(string key);
    Task<BlockStateSet<T>> GetBestChainBlockStateSetAsync(string key);
    Task SetBestChainBlockStateSetAsync(string key, string blockHash);
    Task SetLongestChainBlockStateSetAsync(string key, string blockHash);
    Task SetBlockStateSetProcessedAsync(string key, string blockHash, bool processed);
    Task SetCurrentBlockStateSetAsync(string key, BlockStateSet<T> blockStateSet);
    Task CleanBlockStateSetsAsync(string key, long blockHeight,string blockHash);
    Task SaveDataAsync(string key);
}