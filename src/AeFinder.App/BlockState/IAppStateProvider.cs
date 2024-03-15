
using AeFinder.Grains.Grain.BlockStates;

namespace AeFinder.App.BlockState;

public interface IAppStateProvider
{
    Task<T> GetLastIrreversibleStateAsync<T>(string chainId, string key);
    Task SetLastIrreversibleStateAsync(string chainId, string key, string value);
    Task MergeStateAsync(string chainId, List<BlockStateSet> blockStateSets);
}
