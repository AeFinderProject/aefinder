
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Sdk;

namespace AeFinder.App.BlockState;

public interface IAppStateProvider
{
    Task<T> GetStateAsync<T>(string chainId, string stateKey, IBlockIndex branchBlockIndex);
    Task<object> GetStateAsync(string chainId, string stateKey, IBlockIndex branchBlockIndex);
    Task<T> GetLastIrreversibleStateAsync<T>(string chainId, string key);
    Task<object> GetLastIrreversibleStateAsync(string chainId, string key);
    Task SetLastIrreversibleStateAsync(string chainId, string key, Grains.State.BlockStates.AppState state);
    Task MergeStateAsync(string chainId, List<BlockStateSet> blockStateSets);
}
