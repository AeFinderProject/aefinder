using AElfIndexer.Grains.State.BlockScanExecution;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScanExecution;

public class BlockScanManagerGrain : Grain<BlockScanManagerState>, IBlockScanManagerGrain
{
    public Task<List<string>> GetBlockScanIdsByChainAsync(string chainId)
    {
        return Task.FromResult(State.BlockScanIds.TryGetValue(chainId, out var ids) ? ids.ToList() : new List<string>());
    }

    public Task<Dictionary<string, HashSet<string>>> GetAllBlockScanIdsAsync()
    {
        return Task.FromResult(State.BlockScanIds);
    }

    public async Task AddBlockScanAsync(string chainId, string blockScanId)
    {
        if (!State.BlockScanIds.TryGetValue(chainId, out var clientIds))
        {
            clientIds = new HashSet<string>();
        }

        clientIds.Add(blockScanId);
        State.BlockScanIds[chainId] = clientIds;
        await WriteStateAsync();
    }

    public async Task RemoveBlockScanAsync(string chainId, string blockScanId)
    {
        if (State.BlockScanIds.TryGetValue(chainId, out var clientIds))
        {
            if (clientIds.Remove(blockScanId))
            {
                State.BlockScanIds[chainId] = clientIds;
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