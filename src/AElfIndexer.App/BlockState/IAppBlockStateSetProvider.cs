using AElfIndexer.Grains.Grain.BlockStates;

namespace AElfIndexer.App.BlockState;

public interface IAppBlockStateSetProvider
{
    Task InitializeAsync(string chainId);
    Task AddBlockStateSetAsync(string chainId, BlockStateSet blockStateSet);
    Task UpdateBlockStateSetAsync(string chainId, BlockStateSet blockStateSet);
    Task<BlockStateSet> GetLongestChainBlockStateSetAsync(string chainId);
    Task<BlockStateSet> GetBestChainBlockStateSetAsync(string chainId);
    Task<BlockStateSet> GetLastIrreversibleBlockStateSetAsync(string chainId);
    Task SetBestChainBlockStateSetAsync(string chainId, string blockHash);
    Task SetLongestChainBlockStateSetAsync(string chainId, string blockHash);
    Task SetLastIrreversibleBlockStateSetAsync(string chainId, string blockHash);
    void CleanBlockStateSets(string chainId, long blockHeight, string blockHash);
    Task<BlockStateSet> GetBlockStateSetAsync(string chainId, string blockHash);
    Task<int> GetBlockStateSetCountAsync(string chainId);
    Task SaveDataAsync(string chainId);
}