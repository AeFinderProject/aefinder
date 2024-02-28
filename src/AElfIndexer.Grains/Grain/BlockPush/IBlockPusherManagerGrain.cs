using Orleans;

namespace AElfIndexer.Grains.Grain.BlockPush;

public interface IBlockPusherManagerGrain : IGrainWithIntegerKey
{
    Task<List<string>> GetBlockPusherIdsByChainAsync(string chainId);
    Task<Dictionary<string, HashSet<string>>> GetAllBlockPusherIdsAsync();
    Task AddBlockPusherAsync(string chainId, string blockPusherId);
    Task RemoveBlockPusherAsync(string chainId, string blockPusherId);
}