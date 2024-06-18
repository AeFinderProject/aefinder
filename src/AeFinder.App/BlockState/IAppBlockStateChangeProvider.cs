using AeFinder.Grains.State.BlockStates;

namespace AeFinder.App.BlockState;

public interface IAppBlockStateChangeProvider
{
    Task AddBlockStateChangeAsync(string chainId, Dictionary<long,List<BlockStateChange>> changeKeys);
    Task<List<BlockStateChange>> GetBlockStateChangeAsync(string chainId, long blockHeight);
    Task CleanBlockStateChangeAsync(string chainId, long libHeight);
}