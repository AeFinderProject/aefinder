using AElfIndexer.Grains.State.Client;
using Orleans;

namespace AElfIndexer.Grains.Grain.Client;

public interface IBlockStateSetsGrain<T> : IGrainWithStringKey
{
    Task<Dictionary<string, BlockStateSet<T>>> GetBlockStateSets();
    // Task<bool> TryGetBlockStateSet(string blockHash, out BlockStateSet<T> blockStateSet);
    Task<Dictionary<string, string>> GetBestChainHashes();
    Task SetBestChainHashes(Dictionary<string,string> bestChainHashes);
    Task<bool> TryAddBlockStateSet(BlockStateSet<T> blockStateSet);
    Task<bool> HasFork();
    Task SetBlockStateSet(BlockStateSet<T> blockStateSet);
    Task<BlockStateSet<T>> GetCurrentBlockStateSet();
    Task CleanBlockStateSets(long blockHeight,string blockHash);
    Task Initialize();
}