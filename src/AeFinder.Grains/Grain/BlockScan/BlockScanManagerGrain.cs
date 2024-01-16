using AeFinder.Grains.State.BlockScan;
using Orleans;

namespace AeFinder.Grains.Grain.BlockScan;

public class BlockScanManagerGrain : Grain<ClientManagerState>, IBlockScanManagerGrain
{
    public Task<List<string>> GetBlockScanIdsByChainAsync(string chainId)
    {
        return Task.FromResult(State.ClientIds.TryGetValue(chainId, out var ids) ? ids.ToList() : new List<string>());
    }

    public Task<Dictionary<string, HashSet<string>>> GetAllBlockScanIdsAsync()
    {
        return Task.FromResult(State.ClientIds);
    }

    public async Task AddBlockScanAsync(string chainId, string blockScanId)
    {
        if (!State.ClientIds.TryGetValue(chainId, out var clientIds))
        {
            clientIds = new HashSet<string>();
        }

        clientIds.Add(blockScanId);
        State.ClientIds[chainId] = clientIds;
        await WriteStateAsync();
    }

    public async Task RemoveBlockScanAsync(string chainId, string blockScanId)
    {
        if (State.ClientIds.TryGetValue(chainId, out var clientIds))
        {
            if (clientIds.Remove(blockScanId))
            {
                State.ClientIds[chainId] = clientIds;
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