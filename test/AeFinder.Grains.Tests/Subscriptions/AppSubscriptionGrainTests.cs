using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Subscriptions;
using Shouldly;
using Xunit;

namespace AeFinder.Grains.Subscriptions;

[Collection(ClusterCollection.Name)]
public class AppSubscriptionGrainTests : AeFinderGrainTestBase
{
    [Fact]
    public async Task App_Test()
    {
        var appId = "AppId";
        var chainId = "AELF";
        var subscriptionManifest1 = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 21
                }
            }
        };
        
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var version1 = (await appGrain.AddSubscriptionAsync(subscriptionManifest1, new byte[] { })).NewVersion;
        
        await appGrain.UpgradeVersionAsync();

        var subscription1 = await appGrain.GetSubscriptionAsync(version1);
        subscription1.SubscriptionItems[0].ChainId.ShouldBe(chainId);
        subscription1.SubscriptionItems[0].StartBlockNumber.ShouldBe(21);

        var subscriptionManifest2 = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 22
                }
            }
        };
        
        var version2 = (await appGrain.AddSubscriptionAsync(subscriptionManifest2, new byte[] { })).NewVersion;
        var id1 = GrainIdHelper.GenerateBlockPusherGrainId(appId, version1, chainId);
        var pusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id1);
        var scanToken1 = Guid.NewGuid().ToString("N");
        await pusherInfoGrain.InitializeAsync(appId, version1, subscriptionManifest1.SubscriptionItems[0], scanToken1);

        var id2 = GrainIdHelper.GenerateBlockPusherGrainId(appId, version2, chainId);
        var blockScanGrain2 = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id2);
        var scanToken2 = Guid.NewGuid().ToString("N");
        await blockScanGrain2.InitializeAsync(appId, version2, subscriptionManifest1.SubscriptionItems[0], scanToken2);

        
        var subscription2 = await appGrain.GetSubscriptionAsync(version2);
        subscription2.SubscriptionItems[0].ChainId.ShouldBe(chainId);
        subscription2.SubscriptionItems[0].StartBlockNumber.ShouldBe(22);
        
        var isAvailable = await appGrain.IsRunningAsync(version1, chainId,scanToken1);
        isAvailable.ShouldBeFalse();
        isAvailable = await appGrain.IsRunningAsync(version2, chainId,scanToken2);
        isAvailable.ShouldBeFalse();

        var allSubscriptionsAsync = await appGrain.GetAllSubscriptionAsync();
        allSubscriptionsAsync.CurrentVersion.Version.ShouldBe(version1);
        allSubscriptionsAsync.NewVersion.Version.ShouldBe(version2);

        var versionStatus = await appGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Initialized);
        versionStatus = await appGrain.GetSubscriptionStatusAsync(version2);
        versionStatus.ShouldBe(SubscriptionStatus.Initialized);

        await appGrain.StartAsync(version1);
        versionStatus = await appGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Started);
        
        isAvailable = await appGrain.IsRunningAsync(version1, chainId, scanToken1);
        isAvailable.ShouldBeTrue();
        isAvailable = await appGrain.IsRunningAsync(Guid.NewGuid().ToString(), chainId, scanToken1);
        isAvailable.ShouldBeFalse();
        isAvailable = await appGrain.IsRunningAsync(null, chainId, scanToken1);
        isAvailable.ShouldBeFalse();
        isAvailable = await appGrain.IsRunningAsync(string.Empty, chainId, scanToken1);
        isAvailable.ShouldBeFalse();
        isAvailable = await appGrain.IsRunningAsync(version1, chainId, scanToken2);
        isAvailable.ShouldBeFalse();
        
        isAvailable = await appGrain.IsRunningAsync(version2, chainId, scanToken2);
        isAvailable.ShouldBeFalse();

        await appGrain.UpgradeVersionAsync();
        allSubscriptionsAsync = await appGrain.GetAllSubscriptionAsync();
        allSubscriptionsAsync.CurrentVersion.Version.ShouldBe(version2);
        allSubscriptionsAsync.NewVersion.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateSubscriptionTest()
    {
        var appId = "AppId";
        var subscriptionManifest1 = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = "tDVV",
                    OnlyConfirmed = false,
                    StartBlockNumber = 1009,
                    LogEventConditions = new List<LogEventCondition>
                    {
                        new()
                        {
                            ContractAddress = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                            EventNames = new List<string> { "Transfer" }
                        }
                    }
                }
            }
        };
        
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var version1 = (await appGrain.AddSubscriptionAsync(subscriptionManifest1, new byte[] { })).NewVersion;
        
        var subscription1 = await appGrain.GetSubscriptionAsync(version1);
        subscription1.SubscriptionItems[0].ChainId.ShouldBe("tDVV");
        subscription1.SubscriptionItems[0].OnlyConfirmed.ShouldBe(false);
        subscription1.SubscriptionItems[0].StartBlockNumber.ShouldBe(1009);
        subscription1.SubscriptionItems[0].LogEventConditions.Count.ShouldBe(1);
        subscription1.SubscriptionItems[0].LogEventConditions[0].ContractAddress.ShouldBe("7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX");
        subscription1.SubscriptionItems[0].LogEventConditions[0].EventNames.Count.ShouldBe(1);

        var subscriptionManifest2 = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = "tDVV",
                    OnlyConfirmed = false,
                    StartBlockNumber = 1009,
                    LogEventConditions = new List<LogEventCondition>()
                    {
                        new LogEventCondition()
                        {
                            ContractAddress = "UYdd84gLMsVdHrgkr3ogqe1ukhKwen8oj32Ks4J1dg6KH9PYC",
                            EventNames = new List<string>() { "Transfer", "ManagerApproved" }
                        }
                    }
                },
                new()
                {
                    ChainId = "AELF",
                    OnlyConfirmed = true,
                    StartBlockNumber = 999
                }
            }
        };

        await appGrain.UpdateSubscriptionAsync(version1, subscriptionManifest2);
        
        var subscription2 = await appGrain.GetSubscriptionAsync(version1);
        subscription2.SubscriptionItems.Count.ShouldBe(2);
        subscription2.SubscriptionItems[0].ChainId.ShouldBe("tDVV");
        subscription2.SubscriptionItems[0].OnlyConfirmed.ShouldBe(false);
        subscription2.SubscriptionItems[0].StartBlockNumber.ShouldBe(1009);
        subscription2.SubscriptionItems[0].LogEventConditions.Count.ShouldBe(1);
        subscription2.SubscriptionItems[0].LogEventConditions[0].ContractAddress.ShouldBe("UYdd84gLMsVdHrgkr3ogqe1ukhKwen8oj32Ks4J1dg6KH9PYC");
        subscription2.SubscriptionItems[0].LogEventConditions[0].EventNames.Count.ShouldBe(2);
        subscription2.SubscriptionItems[1].ChainId.ShouldBe("AELF");
        subscription2.SubscriptionItems[1].OnlyConfirmed.ShouldBe(true);
        subscription2.SubscriptionItems[1].StartBlockNumber.ShouldBe(999);
    }

    [Fact]
    public async Task Stop_Test()
    {
        var appId = "AppId";
        var chainId = "AELF";
        var subscriptionManifest = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 21
                }
            }
        };
        
        var scanAppGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var version1 = (await scanAppGrain.AddSubscriptionAsync(subscriptionManifest, new byte[] { })).NewVersion;
        
        var subscription1 = await scanAppGrain.GetSubscriptionAsync(version1);
        subscription1.SubscriptionItems.Count.ShouldBe(1);
        subscription1.SubscriptionItems[0].ChainId.ShouldBe(chainId);
        subscription1.SubscriptionItems[0].StartBlockNumber.ShouldBe(21);

        var subscriptionManifest2 = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 22
                }
            }
        };
        
        var version2 = (await scanAppGrain.AddSubscriptionAsync(subscriptionManifest2, new byte[] { })).NewVersion;

        var id1 = GrainIdHelper.GenerateBlockPusherGrainId(appId, version1, chainId);
        var blockScanGrain1 = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id1);
        var scanToken1 = Guid.NewGuid().ToString("N");
        await blockScanGrain1.InitializeAsync(appId, version1, subscriptionManifest.SubscriptionItems[0], scanToken1);

        var id2 = GrainIdHelper.GenerateBlockPusherGrainId(appId, version2, chainId);
        var blockScanGrain2 = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id2);
        var scanToken2 = Guid.NewGuid().ToString("N");
        await blockScanGrain2.InitializeAsync(appId, version2, subscriptionManifest2.SubscriptionItems[0], scanToken2);

        await scanAppGrain.StopAsync(version1);
        var version = await scanAppGrain.GetAllSubscriptionAsync();
        version.CurrentVersion.ShouldBeNull();
        
        await scanAppGrain.StopAsync(version2);
        version = await scanAppGrain.GetAllSubscriptionAsync();
        version.NewVersion.ShouldBeNull();
    }
}