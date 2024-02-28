using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.Subscriptions;

public interface ISubscriptionGrain: IGrainWithStringKey
{
    Task SetSubscriptionAsync(SubscriptionManifest subscriptionManifest);
    Task<SubscriptionManifest> GetSubscriptionAsync();
    Task RemoveAsync();
}