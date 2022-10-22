using System.Threading.Tasks;
using Orleans;

namespace AElfScan.ScanClients;

public class ClientGrain : Grain<ClientState>, IClientGrain
{

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

    public Task<ClientInfo> GetClientInfoAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task<SubscribeInfo> GetSubscribeInfoAsync()
    {
        throw new System.NotImplementedException();
    }

    public Task SetScanNewBlockStartHeightAsync(long height)
    {
        throw new System.NotImplementedException();
    }

    public Task<string> InitAsync(string chainId, string clientId, SubscribeInfo info)
    {
        throw new System.NotImplementedException();
    }
}