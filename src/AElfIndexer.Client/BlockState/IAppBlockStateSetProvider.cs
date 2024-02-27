using AElfIndexer.Grains.Grain.BlockStates;

namespace AElfIndexer.Client.BlockState;

public interface IAppBlockStateSetProvider
{
    Task InitializeAsync(string chainId);
    Task<Dictionary<string, AppBlockStateSet>> GetBlockStateSetsAsync(string chainId);
    Task AddBlockStateSetAsync(string chainId, AppBlockStateSet blockStateSet);
    Task UpdateBlockStateSetAsync(string chainId, AppBlockStateSet blockStateSet);
    Task<AppBlockStateSet> GetLongestChainBlockStateSetAsync(string chainId);
    Task<AppBlockStateSet> GetBestChainBlockStateSetAsync(string chainId);
    Task SetBestChainBlockStateSetAsync(string chainId, string blockHash);
    Task SetLongestChainBlockStateSetAsync(string chainId, string blockHash);
    Task SetLastIrreversibleBlockStateSetAsync(string chainId, string blockHash);
    Task SaveDataAsync(string chainId);
}