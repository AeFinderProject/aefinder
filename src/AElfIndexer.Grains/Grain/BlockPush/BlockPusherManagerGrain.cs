using AElfIndexer.Grains.State.BlockPush;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockPush;

public class BlockPusherManagerGrain : Grain<BlockPusherManagerState>, IBlockPusherManagerGrain
{
    public Task<List<string>> GetBlockPusherIdsByChainAsync(string chainId)
    {
        return Task.FromResult(State.BlockPusherIds.TryGetValue(chainId, out var ids) ? ids.ToList() : new List<string>());
    }

    public Task<Dictionary<string, HashSet<string>>> GetAllBlockPusherIdsAsync()
    {
        return Task.FromResult(State.BlockPusherIds);
    }

    public async Task AddBlockPusherAsync(string chainId, string blockPusherId)
    {
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
        if (State.BlockPusherIds.TryGetValue(chainId, out var clientIds))
        {
            if (clientIds.Remove(blockPusherId))
            {
                State.BlockPusherIds[chainId] = clientIds;
                await WriteStateAsync();
            }
        }
    }

    public override async Task OnActivateAsync()
    {
        await this.ReadStateAsync();
        await base.OnActivateAsync();
    }
}