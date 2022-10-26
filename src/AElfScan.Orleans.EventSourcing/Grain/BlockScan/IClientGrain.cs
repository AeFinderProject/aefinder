using Orleans;

namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public interface IClientGrain : IGrainWithStringKey
{
    Task<ClientInfo> GetClientInfoAsync();
    Task<SubscribeInfo> GetSubscribeInfoAsync();
    Task SetScanNewBlockStartHeightAsync(long height);
    Task InitializeAsync(string chainId, string clientId, string version, SubscribeInfo info);
}