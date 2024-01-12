using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.BlockScanExecution;
using AElfIndexer.Grains.Grain.ScanApps;
using AElfIndexer.Grains.State.ScanApps;
using AElfIndexer.Grains.State.Subscriptions;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.BlockScan;

[Collection(ClusterCollection.Name)]
public class ScanAppGrainTests : AElfIndexerGrainTestBase
{
    [Fact]
    public async Task Client_Test()
    {
        var clientId = "client";
        var chainId = "AELF";
        var subscriptionInfo1 = new Subscription
        {
            SubscriptionItems = new List<SubscriptionItem>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 21
                }
            }
        };
        
        var scanAppGrain = Cluster.Client.GetGrain<IScanAppGrain>(GrainIdHelper.GenerateScanAppGrainId(clientId));
        var version1 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo1);
        
        await scanAppGrain.UpgradeVersionAsync();

        var subscription1 = await scanAppGrain.GetSubscriptionAsync(version1);
        subscription1.SubscriptionItems[0].ChainId.ShouldBe(chainId);
        subscription1.SubscriptionItems[0].StartBlockNumber.ShouldBe(21);

        var subscriptionInfo2 = new Subscription
        {
            SubscriptionItems = new List<SubscriptionItem>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 22
                }
            }
        };
        
        var version2 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo2);
        var id1 = GrainIdHelper.GenerateBlockScanGrainId(clientId, version1, chainId);
        var blockScanGrain1 = Cluster.Client.GetGrain<IBlockScanGrain>(id1);
        var scanToken1 = Guid.NewGuid().ToString("N");
        await blockScanGrain1.InitializeAsync(clientId, version1, subscriptionInfo1.SubscriptionItems[0], scanToken1);

        var id2 = GrainIdHelper.GenerateBlockScanGrainId(clientId, version2, chainId);
        var blockScanGrain2 = Cluster.Client.GetGrain<IBlockScanGrain>(id2);
        var scanToken2 = Guid.NewGuid().ToString("N");
        await blockScanGrain2.InitializeAsync(clientId, version2, subscriptionInfo1.SubscriptionItems[0], scanToken2);

        
        var subscription2 = await scanAppGrain.GetSubscriptionAsync(version2);
        subscription2.SubscriptionItems[0].ChainId.ShouldBe(chainId);
        subscription2.SubscriptionItems[0].StartBlockNumber.ShouldBe(22);
        
        var isAvailable = await scanAppGrain.IsRunningAsync(version1, chainId,scanToken1);
        isAvailable.ShouldBeFalse();
        isAvailable = await scanAppGrain.IsRunningAsync(version2, chainId,scanToken2);
        isAvailable.ShouldBeFalse();

        var allSubscriptionsAsync = await scanAppGrain.GetAllSubscriptionAsync();
        allSubscriptionsAsync.CurrentVersion.Version.ShouldBe(version1);
        allSubscriptionsAsync.NewVersion.Version.ShouldBe(version2);

        var versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Initialized);
        versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version2);
        versionStatus.ShouldBe(SubscriptionStatus.Initialized);

        await scanAppGrain.StartAsync(version1);
        versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Started);
        
        isAvailable = await scanAppGrain.IsRunningAsync(version1, chainId, scanToken1);
        isAvailable.ShouldBeTrue();
        isAvailable = await scanAppGrain.IsRunningAsync(Guid.NewGuid().ToString(), chainId, scanToken1);
        isAvailable.ShouldBeFalse();
        isAvailable = await scanAppGrain.IsRunningAsync(null, chainId, scanToken1);
        isAvailable.ShouldBeFalse();
        isAvailable = await scanAppGrain.IsRunningAsync(string.Empty, chainId, scanToken1);
        isAvailable.ShouldBeFalse();
        isAvailable = await scanAppGrain.IsRunningAsync(version1, chainId, scanToken2);
        isAvailable.ShouldBeFalse();
        
        isAvailable = await scanAppGrain.IsRunningAsync(version2, chainId, scanToken2);
        isAvailable.ShouldBeFalse();

        await scanAppGrain.UpgradeVersionAsync();
        allSubscriptionsAsync = await scanAppGrain.GetAllSubscriptionAsync();
        allSubscriptionsAsync.CurrentVersion.Version.ShouldBe(version2);
        allSubscriptionsAsync.NewVersion.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateSubscriptionInfo_Test()
    {
        var clientId = "client-id01";
        var subscriptionInfo1 = new Subscription
        {
            SubscriptionItems = new List<SubscriptionItem>()
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
        
        var scanAppGrain = Cluster.Client.GetGrain<IScanAppGrain>(GrainIdHelper.GenerateScanAppGrainId(clientId));
        var version1 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo1);
        
        var subscription1 = await scanAppGrain.GetSubscriptionAsync(version1);
        subscription1.SubscriptionItems[0].ChainId.ShouldBe("tDVV");
        subscription1.SubscriptionItems[0].OnlyConfirmed.ShouldBe(false);
        subscription1.SubscriptionItems[0].StartBlockNumber.ShouldBe(1009);
        subscription1.SubscriptionItems[0].LogEventConditions.Count.ShouldBe(1);
        subscription1.SubscriptionItems[0].LogEventConditions[0].ContractAddress.ShouldBe("7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX");
        subscription1.SubscriptionItems[0].LogEventConditions[0].EventNames.Count.ShouldBe(1);

        var subscriptionInfo2 = new Subscription
        {
            SubscriptionItems = new List<SubscriptionItem>()
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

        await scanAppGrain.UpdateSubscriptionAsync(version1, subscriptionInfo2);
        
        var subscription2 = await scanAppGrain.GetSubscriptionAsync(version1);
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
        var clientId = "client";
        var chainId = "AELF";
        var subscriptionInfo1 = new Subscription
        {
            SubscriptionItems = new List<SubscriptionItem>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 21
                }
            }
        };
        
        var scanAppGrain = Cluster.Client.GetGrain<IScanAppGrain>(GrainIdHelper.GenerateScanAppGrainId(clientId));
        var version1 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo1);
        
        var subscription1 = await scanAppGrain.GetSubscriptionAsync(version1);
        subscription1.SubscriptionItems.Count.ShouldBe(1);
        subscription1.SubscriptionItems[0].ChainId.ShouldBe(chainId);
        subscription1.SubscriptionItems[0].StartBlockNumber.ShouldBe(21);

        var subscriptionInfo2 = new Subscription
        {
            SubscriptionItems = new List<SubscriptionItem>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 22
                }
            }
        };
        
        var version2 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo2);

        var id1 = GrainIdHelper.GenerateBlockScanGrainId(clientId, version1, chainId);
        var blockScanGrain1 = Cluster.Client.GetGrain<IBlockScanGrain>(id1);
        var scanToken1 = Guid.NewGuid().ToString("N");
        await blockScanGrain1.InitializeAsync(clientId, version1, subscriptionInfo1.SubscriptionItems[0], scanToken1);

        var id2 = GrainIdHelper.GenerateBlockScanGrainId(clientId, version2, chainId);
        var blockScanGrain2 = Cluster.Client.GetGrain<IBlockScanGrain>(id2);
        var scanToken2 = Guid.NewGuid().ToString("N");
        await blockScanGrain2.InitializeAsync(clientId, version2, subscriptionInfo2.SubscriptionItems[0], scanToken2);

        await scanAppGrain.StopAsync(version1);
        var version = await scanAppGrain.GetAllSubscriptionAsync();
        version.CurrentVersion.ShouldBeNull();
        
        await scanAppGrain.StopAsync(version2);
        version = await scanAppGrain.GetAllSubscriptionAsync();
        version.NewVersion.ShouldBeNull();
    }
}