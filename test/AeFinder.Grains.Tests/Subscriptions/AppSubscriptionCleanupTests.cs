using System;
using System.Threading.Tasks;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Subscriptions;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace AeFinder.Grains.Subscriptions;

[Collection(ClusterCollection.Name)]
public class AppSubscriptionCleanupTests
{
    private readonly ClusterFixture _fixture;

    public AppSubscriptionCleanupTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Stop_Should_Reject_Missing_Manifest_Before_Changing_Version(bool pending, bool missingItems)
    {
        var grain = _fixture.Cluster.Client.GetGrain<IAppSubscriptionGrain>(Guid.NewGuid().ToString("N"));
        var current = (await grain.AddSubscriptionAsync(new SubscriptionManifest(), new byte[] { })).NewVersion;
        var pendingVersion = (await grain.AddSubscriptionAsync(new SubscriptionManifest(), new byte[] { })).NewVersion;
        var version = pending ? pendingVersion : current;
        await grain.UpdateSubscriptionAsync(version,
            missingItems ? new SubscriptionManifest { SubscriptionItems = null } : null);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => grain.StopAsync(version));

        exception.Message.ShouldBe("Subscription manifest is missing. Run destroy before stop.");
        var subscriptions = await grain.GetAllSubscriptionAsync();
        subscriptions.CurrentVersion.Version.ShouldBe(current);
        subscriptions.PendingVersion.Version.ShouldBe(pendingVersion);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stop_Should_Remove_Version_After_Cleanup_Manifest_Is_Saved(bool hasPusher)
    {
        var appId = Guid.NewGuid().ToString("N");
        var grain = _fixture.Cluster.Client.GetGrain<IAppSubscriptionGrain>(appId);
        var version = (await grain.AddSubscriptionAsync(null, new byte[] { })).NewVersion;
        var manifest = new SubscriptionManifest();
        var manager = _fixture.Cluster.Client.GetGrain<IBlockPusherManagerGrain>(0);
        var pusherId = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, "AELF");
        if (hasPusher)
        {
            var subscription = new Subscription { ChainId = "AELF" };
            var pusher = _fixture.Cluster.Client.GetGrain<IBlockPusherInfoGrain>(pusherId);
            await pusher.InitializeAsync(appId, version, subscription, "cleanup-token");
            await manager.AddBlockPusherAsync("AELF", pusherId);
            manifest.SubscriptionItems.Add(subscription);
        }
        await grain.UpdateSubscriptionAsync(version, manifest);

        await grain.StopAsync(version);
        await grain.StopAsync(version);

        var subscriptions = await grain.GetAllSubscriptionAsync();
        subscriptions.CurrentVersion.ShouldBeNull();
        subscriptions.PendingVersion.ShouldBeNull();
        (await manager.GetBlockPusherIdsByChainAsync("AELF")).ShouldNotContain(pusherId);
    }
}
