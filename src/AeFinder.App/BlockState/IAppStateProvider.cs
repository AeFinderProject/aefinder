
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Sdk;

namespace AeFinder.App.BlockState;

public interface IAppStateProvider
{
    Task<T> GetStateAsync<T>(string chainId, string stateKey, IBlockIndex branchBlockIndex);
    Task<object> GetStateAsync(string chainId, string stateKey, IBlockIndex branchBlockIndex);
    Task PreMergeStateAsync(string chainId, List<BlockStateSet> blockStateSets);
    Task MergeStateAsync(string chainId, List<BlockStateSet> blockStateSets);
}
