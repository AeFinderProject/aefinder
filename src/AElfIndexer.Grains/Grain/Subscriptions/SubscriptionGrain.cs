using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.Subscriptions;

public class SubscriptionGrain : Grain<SubscriptionState>, ISubscriptionGrain
{
    public async Task SetSubscriptionAsync(Subscription subscription)
    {
        State.Subscription = subscription;
        await WriteStateAsync();
    }

    public async Task<Subscription> GetSubscriptionAsync()
    {
        await ReadStateAsync();
        return State.Subscription;
    }

    public async Task RemoveAsync()
    {
        await ClearStateAsync();
    }
}