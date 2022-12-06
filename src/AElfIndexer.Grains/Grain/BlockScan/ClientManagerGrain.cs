using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class ClientManagerGrain : Grain<ClientManagerState>, IClientManagerGrain
{
    public Task<List<string>> GetClientIdsByChainAsync(string chainId)
    {
        return Task.FromResult(State.ClientIds.TryGetValue(chainId, out var ids) ? ids.ToList() : new List<string>());
    }

    public Task<Dictionary<string, HashSet<string>>> GetAllClientIdsAsync()
    {
        return Task.FromResult(State.ClientIds);
    }

    public async Task AddClientAsync(string chainId, string clientId)
    {
        if (!State.ClientIds.TryGetValue(chainId, out var clientIds))
        {
            clientIds = new HashSet<string>();
        }

        clientIds.Add(clientId);
        State.ClientIds[chainId] = clientIds;
        await WriteStateAsync();
    }

    public async Task RemoveClientAsync(string chainId, string clientId)
    {
        if (State.ClientIds.TryGetValue(chainId, out var clientIds))
        {
            if (clientIds.Remove(clientId))
            {
                State.ClientIds[chainId] = clientIds;
                await WriteStateAsync();
            }
        }
    }

    public override Task OnActivateAsync()
    {
        this.ReadStateAsync();
        return base.OnActivateAsync();
    }

    public override Task OnDeactivateAsync()
    {
        this.WriteStateAsync();
        return base.OnDeactivateAsync();
    }
}