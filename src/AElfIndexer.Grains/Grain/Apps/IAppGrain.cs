using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.Apps;

public interface IAppGrain: IGrainWithStringKey
{
    Task<string> AddSubscriptionAsync(SubscriptionManifest subscriptionManifest);
    Task UpdateSubscriptionAsync(string version, SubscriptionManifest subscriptionManifest);
    Task<SubscriptionManifest> GetSubscriptionAsync(string version);
    Task<AllSubscription> GetAllSubscriptionAsync();
    Task<byte[]> GetCodeAsync(string version);
    Task<bool> IsRunningAsync(string version, string chainId, string scanToken);
    Task UpgradeVersionAsync();
    Task<SubscriptionStatus> GetSubscriptionStatusAsync(string version);
    Task StartAsync(string version);
    Task PauseAsync(string version);
    Task StopAsync(string version);
}