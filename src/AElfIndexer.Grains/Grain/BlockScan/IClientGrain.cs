using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Orleans;

namespace AElfIndexer.Grains.Grain.BlockScan;

public interface IClientGrain: IGrainWithStringKey
{
    Task<string> AddSubscriptionInfoAsync(List<SubscriptionInfo> subscriptionInfos);
    Task<List<SubscriptionInfo>> GetSubscriptionInfoAsync(string version);
    Task AddBlockScanIdAsync(string version, string id);
    Task<List<string>> GetBlockScanIdsAsync(string version);
    Task<bool> IsVersionAvailableAsync(string version);
    Task<string> GetCurrentVersionAsync();
    Task<string> GetNewVersionAsync();
    Task UpgradeVersionAsync();
    Task RemoveVersionInfoAsync(string version);
    Task<VersionStatus> GetVersionStatus(string version);
    Task StartAsync(string version);
}