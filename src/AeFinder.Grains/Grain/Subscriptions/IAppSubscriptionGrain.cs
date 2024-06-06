using AeFinder.Subscriptions;
using Orleans;

namespace AeFinder.Grains.Grain.Subscriptions;

public interface IAppSubscriptionGrain : IGrainWithStringKey
{
    Task<AddSubscriptionDto> AddSubscriptionAsync(SubscriptionManifest subscriptionManifest, byte[] code);
    Task UpdateSubscriptionAsync(string version, SubscriptionManifest subscriptionManifest);
    Task UpdateCodeAsync(string version, byte[] code);
    Task<SubscriptionManifest> GetSubscriptionAsync(string version);
    Task<AllSubscription> GetAllSubscriptionAsync();
    Task<byte[]> GetCodeAsync(string version);
    Task<bool> IsRunningAsync(string version, string chainId, string pushToken);
    Task UpgradeVersionAsync();
    Task<SubscriptionStatus> GetSubscriptionStatusAsync(string version);
    Task StartAsync(string version);
    Task PauseAsync(string version);
    Task StopAsync(string version);
}