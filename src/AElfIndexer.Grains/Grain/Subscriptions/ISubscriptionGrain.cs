using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.Subscriptions;

public interface ISubscriptionGrain: IGrainWithStringKey
{
    Task SetSubscriptionAsync(Subscription subscription);
    Task<Subscription> GetSubscriptionAsync();
    Task RemoveAsync();
}