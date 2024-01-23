using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.ScanApps;

public interface IScanAppGrain: IGrainWithStringKey
{
    Task<string> AddSubscriptionAsync(Subscription subscription);
    Task UpdateSubscriptionAsync(string version, Subscription subscription);
    Task<Subscription> GetSubscriptionAsync(string version);
    Task<AllSubscription> GetAllSubscriptionAsync();
    Task<byte[]> GetCodeAsync(string version);
    Task<bool> IsRunningAsync(string version, string chainId, string scanToken);
    Task UpgradeVersionAsync();
    Task<SubscriptionStatus> GetSubscriptionStatusAsync(string version);
    Task StartAsync(string version);
    Task PauseAsync(string version);
    Task StopAsync(string version);
}