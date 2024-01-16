using AeFinder.BlockScan;
using AeFinder.Grains.State.BlockScan;
using Orleans;

namespace AeFinder.Grains.Grain.BlockScan;

public interface IBlockScanInfoGrain : IGrainWithStringKey
{
    Task<ClientInfo> GetClientInfoAsync();
    Task<SubscriptionInfo> GetSubscriptionInfoAsync();
    Task SetScanNewBlockStartHeightAsync(long height);
    Task SetHandleHistoricalBlockTimeAsync(DateTime time);
    Task SetHistoricalBlockScanModeAsync();
    Task InitializeAsync(string chainId, string clientId, string version, SubscriptionInfo info);
    Task UpdateSubscriptionInfoAsync(SubscriptionInfo info);
    Task StopAsync();
    Task<Guid> GetMessageStreamIdAsync();
}