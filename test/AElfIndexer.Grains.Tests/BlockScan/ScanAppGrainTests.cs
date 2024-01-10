using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.BlockScanExecution;
using AElfIndexer.Grains.Grain.ScanApps;
using AElfIndexer.Grains.State.ScanApps;
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
            Items = new Dictionary<string, SubscriptionItem>
            {
                {
                    chainId, new SubscriptionItem
                    {
                        ChainId = chainId,
                        OnlyConfirmed = true,
                        StartBlockNumber = 21
                    }
                }
            }
        };
        
        var scanAppGrain = Cluster.Client.GetGrain<IScanAppGrain>(clientId);
        var version1 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo1);
        
        await scanAppGrain.UpgradeVersionAsync();

        var subscription1 = await scanAppGrain.GetSubscriptionAsync(version1);
        subscription1.Items[chainId].ChainId.ShouldBe(chainId);
        subscription1.Items[chainId].StartBlockNumber.ShouldBe(21);
        
        var subscriptionInfo2 = new Subscription
        {
            Items = new Dictionary<string, SubscriptionItem>
            {
                {
                    chainId, new SubscriptionItem
                    {
                        ChainId = chainId,
                        OnlyConfirmed = true,
                        StartBlockNumber = 22
                    }
                }
            }
        };
        
        var version2 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo2);
        var id1 = GrainIdHelper.GenerateGrainId(chainId, clientId, version1);
        var blockScanGrain1 = Cluster.Client.GetGrain<IBlockScanGrain>(id1);
        var scanToken1 = Guid.NewGuid().ToString("N");
        await blockScanGrain1.InitializeAsync(scanToken1,chainId, clientId, version1, subscriptionInfo1.Items[chainId]);
        
        var id2 = GrainIdHelper.GenerateGrainId(chainId, clientId, version2);
        var blockScanGrain2 = Cluster.Client.GetGrain<IBlockScanGrain>(id2);
        var scanToken2 = Guid.NewGuid().ToString("N");
        await blockScanGrain2.InitializeAsync(scanToken2,chainId, clientId, version2, subscriptionInfo1.Items[chainId]);

        
        var subscription2 = await scanAppGrain.GetSubscriptionAsync(version2);
        subscription2.Items[chainId].ChainId.ShouldBe(chainId);
        subscription2.Items[chainId].StartBlockNumber.ShouldBe(22);
        
        var isAvailable = await scanAppGrain.IsRunningAsync(version1, chainId,scanToken1);
        isAvailable.ShouldBeFalse();
        isAvailable = await scanAppGrain.IsRunningAsync(version2, chainId,scanToken2);
        isAvailable.ShouldBeFalse();

        var allSubscriptionsAsync = await scanAppGrain.GetAllSubscriptionsAsync();
        allSubscriptionsAsync.CurrentVersion.Version.ShouldBe(version1);
        allSubscriptionsAsync.NewVersion.Version.ShouldBe(version2);

        var versionStatus = await scanAppGrain.GetVersionStatusAsync(version1);
        versionStatus.ShouldBe(VersionStatus.Initialized);
        versionStatus = await scanAppGrain.GetVersionStatusAsync(version2);
        versionStatus.ShouldBe(VersionStatus.Initialized);

        await scanAppGrain.StartAsync(version1);
        versionStatus = await scanAppGrain.GetVersionStatusAsync(version1);
        versionStatus.ShouldBe(VersionStatus.Started);
        
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
        allSubscriptionsAsync = await scanAppGrain.GetAllSubscriptionsAsync();
        allSubscriptionsAsync.CurrentVersion.Version.ShouldBe(version2);
        allSubscriptionsAsync.NewVersion.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateSubscriptionInfo_Test()
    {
        var clientId = "client-id01";
        var subscriptionInfo1 = new Subscription
        {
            Items = new Dictionary<string, SubscriptionItem>
            {
                {
                    "tDVV", new SubscriptionItem
                    {
                        ChainId = "tDVV",
                        OnlyConfirmed = false,
                        StartBlockNumber = 1009,
                        LogEventFilters = new List<LogEventFilter>
                        {
                            new LogEventFilter
                            {
                                ContractAddress = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                                EventNames = new List<string>{"Transfer"}
                            }
                        }
                    }
                }
            }
        };
        
        var scanAppGrain = Cluster.Client.GetGrain<IScanAppGrain>(clientId);
        var version1 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo1);
        
        var subscription1 = await scanAppGrain.GetSubscriptionAsync(version1);
        subscription1.Items["tDVV"].ChainId.ShouldBe("tDVV");
        subscription1.Items["tDVV"].OnlyConfirmed.ShouldBe(false);
        subscription1.Items["tDVV"].StartBlockNumber.ShouldBe(1009);
        subscription1.Items["tDVV"].LogEventFilters.Count.ShouldBe(1);
        subscription1.Items["tDVV"].LogEventFilters[0].ContractAddress.ShouldBe("7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX");
        subscription1.Items["tDVV"].LogEventFilters[0].EventNames.Count.ShouldBe(1);
        
        var subscriptionInfo2 = new Subscription
        {
            Items = new Dictionary<string, SubscriptionItem>
            {
                {
                    "tDVV", new SubscriptionItem
                    {
                        ChainId = "tDVV",
                        OnlyConfirmed = false,
                        StartBlockNumber = 1009,
                        LogEventFilters = new List<LogEventFilter>()
                        {
                            new LogEventFilter()
                            {
                                ContractAddress = "UYdd84gLMsVdHrgkr3ogqe1ukhKwen8oj32Ks4J1dg6KH9PYC",
                                EventNames = new List<string>(){"Transfer","ManagerApproved"}
                            }
                        }
                    }
                },
                {
                    "AELF", new SubscriptionItem
                    {
                        ChainId = "AELF",
                        OnlyConfirmed = true,
                        StartBlockNumber = 999
                    }
                }
            }
        };

        await scanAppGrain.UpdateSubscriptionAsync(version1, subscriptionInfo2);
        
        var subscription2 = await scanAppGrain.GetSubscriptionAsync(version1);
        subscription2.Items.Count.ShouldBe(2);
        subscription2.Items["tDVV"].ChainId.ShouldBe("tDVV");
        subscription2.Items["tDVV"].OnlyConfirmed.ShouldBe(false);
        subscription2.Items["tDVV"].StartBlockNumber.ShouldBe(1009);
        subscription2.Items["tDVV"].LogEventFilters.Count.ShouldBe(1);
        subscription2.Items["tDVV"].LogEventFilters[0].ContractAddress.ShouldBe("UYdd84gLMsVdHrgkr3ogqe1ukhKwen8oj32Ks4J1dg6KH9PYC");
        subscription2.Items["tDVV"].LogEventFilters[0].EventNames.Count.ShouldBe(2);
        subscription2.Items["AELF"].ChainId.ShouldBe("AELF");
        subscription2.Items["AELF"].OnlyConfirmed.ShouldBe(true);
        subscription2.Items["AELF"].StartBlockNumber.ShouldBe(999);
    }

    [Fact]
    public async Task Stop_Test()
    {
        var clientId = "client";
        var chainId = "AELF";
        var subscriptionInfo1 = new Subscription
        {
            Items = new Dictionary<string, SubscriptionItem>
            {
                {
                    chainId, new SubscriptionItem
                    {
                        ChainId = chainId,
                        OnlyConfirmed = true,
                        StartBlockNumber = 21
                    }
                }
            }
        };
        
        var scanAppGrain = Cluster.Client.GetGrain<IScanAppGrain>(clientId);
        var version1 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo1);
        
        var subscription1 = await scanAppGrain.GetSubscriptionAsync(version1);
        subscription1.Items.Count.ShouldBe(1);
        subscription1.Items[chainId].ChainId.ShouldBe(chainId);
        subscription1.Items[chainId].StartBlockNumber.ShouldBe(21);
        
        var subscriptionInfo2 = new Subscription
        {
            Items = new Dictionary<string, SubscriptionItem>
            {
                {
                    chainId, new SubscriptionItem
                    {
                        ChainId = chainId,
                        OnlyConfirmed = true,
                        StartBlockNumber = 22
                    }
                }
            }
        };
        
        var version2 = await scanAppGrain.AddSubscriptionAsync(subscriptionInfo2);
        
        var id1 = GrainIdHelper.GenerateGrainId(chainId, clientId, version1);
        var blockScanGrain1 = Cluster.Client.GetGrain<IBlockScanGrain>(id1);
        var scanToken1 = Guid.NewGuid().ToString("N");
        await blockScanGrain1.InitializeAsync(scanToken1,chainId, clientId, version1, subscriptionInfo1.Items[chainId]);
        
        var id2 = GrainIdHelper.GenerateGrainId(chainId, clientId, version2);
        var blockScanGrain2 = Cluster.Client.GetGrain<IBlockScanGrain>(id2);
        var scanToken2 = Guid.NewGuid().ToString("N");
        await blockScanGrain2.InitializeAsync(scanToken2,chainId, clientId, version2, subscriptionInfo2.Items[chainId]);

        await scanAppGrain.StopAsync(version1);
        var version = await scanAppGrain.GetAllSubscriptionsAsync();
        version.CurrentVersion.ShouldBeNull();
        
        await scanAppGrain.StopAsync(version2);
        version = await scanAppGrain.GetAllSubscriptionsAsync();
        version.NewVersion.ShouldBeNull();
    }
}