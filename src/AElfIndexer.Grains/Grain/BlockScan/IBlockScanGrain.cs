using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IBlockScanGrain : IGrainWithStringKey
{
    Task<ScanInfo> GetClientInfoAsync();
    Task<SubscriptionItem> GetSubscriptionInfoAsync();
    Task SetScanNewBlockStartHeightAsync(long height);
    Task SetHandleHistoricalBlockTimeAsync(DateTime time);
    Task SetHistoricalBlockScanModeAsync();
    Task InitializeAsync(string scanToken, string chainId, string clientId, string version, SubscriptionItem item);
    Task UpdateSubscriptionInfoAsync(SubscriptionItem info);
    Task StopAsync();
    Task<Guid> GetMessageStreamIdAsync();
    Task<bool> IsScanBlockAsync(long blockHeight, bool isConfirmed);
    Task<ScanMode> GetScanModeAsync();
    Task<bool> IsNeedRecoverAsync();
    Task<bool> IsRunningAsync(string token);
}