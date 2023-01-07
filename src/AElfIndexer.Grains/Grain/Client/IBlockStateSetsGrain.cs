using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public interface IBlockStateSetsGrain<T> : IGrainWithStringKey
{
    Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSets();
    // Task<bool> TryGetBlockStateSet(string blockHash, out BlockStateSet<T> blockStateSet);
    Task<Dictionary<string, string>> GetLongestChainHashes();
    Task SetLongestChainHashes(Dictionary<string, string> bestChainHashes, bool isChangesReset = false);
    Task AddBlockStateSet(BlockStateSet<T> blockStateSet);
    Task SetBlockStateSet(BlockStateSet<T> blockStateSet);
    Task<BlockStateSet<T>> GetCurrentBlockStateSet();
    Task<BlockStateSet<T>> GetLongestChainBlockStateSet();
    Task<BlockStateSet<T>> GetBestChainBlockStateSet();
    Task SetBestChainBlockStateSet(string blockHash);
    Task SetLongestChainBlockStateSet(string blockHash);
    Task SetBlockStateSetProcessed(string blockHash);
    Task SetCurrentBlockStateSet(BlockStateSet<T> blockStateSet);
    Task CleanBlockStateSets(long blockHeight,string blockHash);
}