using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Orleans;

namespace AElfScan.ScanClients;

public class ClientManagerGrain : Grain<ClientManagerState>, IClientManagerGrain
{
    public Task<List<string>> GetClientIdsAsync(string chainId)
    {
        return Task.FromResult(State.ClientIds[chainId].ToList());
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