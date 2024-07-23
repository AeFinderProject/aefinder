using AeFinder.Grains.State.BlockPush;
using Orleans;

namespace AeFinder.Grains.Grain.BlockPush;

public class BlockPusherManagerGrain : AeFinderGrain<BlockPusherManagerState>, IBlockPusherManagerGrain
{
    public async Task<List<string>> GetBlockPusherIdsByChainAsync(string chainId)
    {
        await ReadStateAsync();
        return State.BlockPusherIds.TryGetValue(chainId, out var ids) ? ids.ToList() : new List<string>();
    }

    public async Task<Dictionary<string, HashSet<string>>> GetAllBlockPusherIdsAsync()
    {
        await ReadStateAsync();
        return State.BlockPusherIds;
    }

    public async Task AddBlockPusherAsync(string chainId, string blockPusherId)
    {
        await ReadStateAsync();
        if (!State.BlockPusherIds.TryGetValue(chainId, out var clientIds))
        {
            clientIds = new HashSet<string>();
        }

        clientIds.Add(blockPusherId);
        State.BlockPusherIds[chainId] = clientIds;
        await WriteStateAsync();
    }

    public async Task RemoveBlockPusherAsync(string chainId, string blockPusherId)
    {
        if (chainId == null)
        {
            return;
        }
        
        await ReadStateAsync();

        if (State.BlockPusherIds.TryGetValue(chainId, out var clientIds))
        {
            if (clientIds.Remove(blockPusherId))
            {
                State.BlockPusherIds[chainId] = clientIds;
                await WriteStateAsync();
            }
        }
    }
}