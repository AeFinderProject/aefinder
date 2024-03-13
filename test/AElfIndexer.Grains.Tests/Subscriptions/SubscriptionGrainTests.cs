using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.Subscriptions;
using AElfIndexer.Grains.State.Subscriptions;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.Subscriptions;

[Collection(ClusterCollection.Name)]
public class SubscriptionGrainTests: AElfIndexerGrainTestBase
{
    [Fact]
    public async Task SetSubscriptionTest()
    {
        var appId = "AppId";
        var version = "Version";
        var subscription = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>
            {
                new Subscription
                {
                    ChainId = "AELF",
                    OnlyConfirmed = false,
                    StartBlockNumber = 100
                }
            }
        };
        var grainId = GrainIdHelper.GenerateGetAppCodeGrainId(appId, version);
        var grain = Cluster.Client.GetGrain<ISubscriptionGrain>(grainId);
        await grain.SetSubscriptionAsync(subscription);
        
        var result = await grain.GetSubscriptionAsync();
        result.SubscriptionItems[0].ChainId.ShouldBe(subscription.SubscriptionItems[0].ChainId);
        result.SubscriptionItems[0].OnlyConfirmed.ShouldBe(subscription.SubscriptionItems[0].OnlyConfirmed);
        result.SubscriptionItems[0].StartBlockNumber.ShouldBe(subscription.SubscriptionItems[0].StartBlockNumber);

        await grain.RemoveAsync();
        
        result = await grain.GetSubscriptionAsync(); 
        result.ShouldBeNull();
    }
}