using AElfScan.Orleans.EventSourcing.State.BlockScan;
using Orleans;

namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public interface IClientGrain : IGrainWithStringKey
{
    Task<ClientInfo> GetClientInfoAsync();
    Task<SubscribeInfo> GetSubscribeInfoAsync();
    Task SetScanNewBlockStartHeightAsync(long height);
    Task SetHandleHistoricalBlockTimeAsync(DateTime time);
    Task InitializeAsync(string chainId, string clientId, string version, SubscribeInfo info);
    Task StopAsync(string version);
}