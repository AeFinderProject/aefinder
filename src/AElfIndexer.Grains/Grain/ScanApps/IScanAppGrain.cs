using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.ScanApps;
using Orleans;

namespace AElfIndexer.Grains.Grain.ScanApps;

public interface IScanAppGrain: IGrainWithStringKey
{
    Task<string> AddSubscriptionAsync(Subscription subscription);
    Task UpdateSubscriptionAsync(string version, Subscription subscription);
    Task<Subscription> GetSubscriptionAsync(string version);
    Task<AllSubscriptionDto> GetAllSubscriptionsAsync();
    Task<bool> IsRunningAsync(string version, string chainId, string scanToken);
    Task UpgradeVersionAsync();
    Task<VersionStatus> GetVersionStatusAsync(string version);
    Task StartAsync(string version);
    Task PauseAsync(string version);
    Task StopAsync(string version);
}