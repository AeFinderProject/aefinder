using AElfIndexer.Grains.State.BlockScanExecution;
using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScanExecution;

public interface IBlockScanGrain : IGrainWithStringKey
{
    Task<ScanInfo> GetScanInfoAsync();
    Task<SubscriptionItem> GetSubscriptionAsync();
    Task SetScanNewBlockStartHeightAsync(long height);
    Task SetHandleHistoricalBlockTimeAsync(DateTime time);
    Task SetHistoricalBlockScanModeAsync();
    Task InitializeAsync(string scanAppId, string version, SubscriptionItem item, string scanToken);
    Task UpdateSubscriptionInfoAsync(SubscriptionItem info);
    Task StopAsync();
    Task<Guid> GetMessageStreamIdAsync();
    Task<bool> IsScanBlockAsync(long blockHeight, bool isConfirmed);
    Task<ScanMode> GetScanModeAsync();
    Task<bool> IsNeedRecoverAsync();
    Task<bool> IsRunningAsync(string token);
    Task<string> GetScanTokenAsync();
}