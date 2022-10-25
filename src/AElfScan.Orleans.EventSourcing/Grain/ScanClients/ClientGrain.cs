using AElfScan.Orleans.EventSourcing.State.ScanClients;
using Orleans;

namespace AElfScan.Orleans.EventSourcing.Grain.ScanClients;

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
        return Task.FromResult(State.ClientInfo);
    }

    public Task<SubscribeInfo> GetSubscribeInfoAsync()
    {
        return Task.FromResult(State.SubscribeInfo);
    }

    public async Task SetScanNewBlockStartHeightAsync(long height)
    {
        State.ClientInfo.ScanModeInfo.ScanMode = ScanMode.NewBlock;
        State.ClientInfo.ScanModeInfo.ScanNewBlockStartHeight = height;
        await WriteStateAsync();
    }

    public async Task<string> InitializeAsync(string chainId, string clientId, SubscribeInfo info)
    {
        var clientGrain = GrainFactory.GetGrain<IClientManagerGrain>(0);
        await clientGrain.AddClientAsync(chainId, clientId);

        State.ClientInfo = new ClientInfo
        {
            ChainId = chainId,
            ClientId = clientId,
            Version = Guid.NewGuid().ToString(),
            ScanModeInfo = new ScanModeInfo
            {
                ScanMode = ScanMode.HistoricalBlock,
                ScanNewBlockStartHeight = 0
            }
        };
        State.SubscribeInfo = info;
        await WriteStateAsync();
        
        return State.ClientInfo.Version;
    }
}