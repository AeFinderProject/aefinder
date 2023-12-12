using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IBlockScanInfoGrain : IGrainWithStringKey
{
    Task<ClientInfo> GetClientInfoAsync();
    Task<SubscriptionItem> GetSubscriptionInfoAsync();
    Task SetScanNewBlockStartHeightAsync(long height);
    Task SetHandleHistoricalBlockTimeAsync(DateTime time);
    Task SetHistoricalBlockScanModeAsync();
    Task InitializeAsync(string chainId, string clientId, string version, SubscriptionItem info);
    Task UpdateSubscriptionInfoAsync(SubscriptionItem info);
    Task StopAsync();
    Task<Guid> GetMessageStreamIdAsync();
}