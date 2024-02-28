using AElfIndexer.Grains.State.Subscriptions;
using Orleans;

namespace AElfIndexer.Grains.Grain.Subscriptions;

public class SubscriptionGrain : Grain<SubscriptionState>, ISubscriptionGrain
{
    public async Task SetSubscriptionAsync(SubscriptionManifest subscriptionManifest)
    {
        State.SubscriptionManifest = subscriptionManifest;
        await WriteStateAsync();
    }

    public async Task<SubscriptionManifest> GetSubscriptionAsync()
    {
        await ReadStateAsync();
        return State.SubscriptionManifest;
    }

    public async Task RemoveAsync()
    {
        await ClearStateAsync();
        DeactivateOnIdle();
    }
}