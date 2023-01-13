using System.Collections.Concurrent;
using AElfIndexer.Grains.State.Client;

namespace AElfIndexer.Client.Providers;

public interface IBlockStateSetProvider<T>
{
    Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSets(string key);
    Task<Dictionary<string, string>> GetLongestChainHashes(string key);
    Task SetLongestChainHashes(string key, Dictionary<string, string> longestChainHashes);
    Task AddBlockStateSet(string key, BlockStateSet<T> blockStateSet);
    Task SetBlockStateSet(string key, BlockStateSet<T> blockStateSet);
    Task<BlockStateSet<T>> GetCurrentBlockStateSet(string key);
    Task<BlockStateSet<T>> GetLongestChainBlockStateSet(string key);
    Task<BlockStateSet<T>> GetBestChainBlockStateSet(string key);
    Task SetBestChainBlockStateSet(string key, string blockHash);
    Task SetLongestChainBlockStateSet(string key, string blockHash);
    Task SetBlockStateSetProcessed(string key, string blockHash);
    Task SetCurrentBlockStateSet(string key, BlockStateSet<T> blockStateSet);
    Task CleanBlockStateSets(string key, long blockHeight,string blockHash);
    Task CommitAsync(string key);
}