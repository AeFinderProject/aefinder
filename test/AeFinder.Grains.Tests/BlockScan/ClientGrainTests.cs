using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Grains.Grain.BlockScan;
using AeFinder.Grains.State.BlockScan;
using Shouldly;
using Xunit;

namespace AeFinder.Grains.BlockScan;

[Collection(ClusterCollection.Name)]
public class ClientGrainTests : AeFinderGrainTestBase
{
    [Fact]
    public async Task Client_Test()
    {
        var clientId = "client";
        var subscriptionInfo1 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Block,
            }
        };
        
        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version1 = await clientGrain.AddSubscriptionInfoAsync(subscriptionInfo1);
        
        await clientGrain.UpgradeVersionAsync();

        var subscription1 = await clientGrain.GetSubscriptionInfoAsync(version1);
        subscription1.Count.ShouldBe(1);
        subscription1[0].ChainId.ShouldBe("AELF");
        subscription1[0].FilterType.ShouldBe(BlockFilterType.Block);
        
        var subscriptionInfo2 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Transaction,
            }
        };
        
        var version2 = await clientGrain.AddSubscriptionInfoAsync(subscriptionInfo2);

        var subscription2 = await clientGrain.GetSubscriptionInfoAsync(version2);
        subscription2.Count.ShouldBe(1);
        subscription2[0].ChainId.ShouldBe("AELF");
        subscription2[0].FilterType.ShouldBe(BlockFilterType.Transaction);
        
        var isAvailable = await clientGrain.IsVersionAvailableAsync(version1);
        isAvailable.ShouldBe(true);
        isAvailable = await clientGrain.IsVersionAvailableAsync(version2);
        isAvailable.ShouldBe(true);
        isAvailable = await clientGrain.IsVersionAvailableAsync(Guid.NewGuid().ToString());
        isAvailable.ShouldBe(false);
        isAvailable = await clientGrain.IsVersionAvailableAsync(null);
        isAvailable.ShouldBe(false);
        isAvailable = await clientGrain.IsVersionAvailableAsync(string.Empty);
        isAvailable.ShouldBe(false);

        var version = await clientGrain.GetVersionAsync();
        version.CurrentVersion.ShouldBe(version1);
        version.NewVersion.ShouldBe(version2);

        var versionStatus = await clientGrain.GetVersionStatusAsync(version1);
        versionStatus.ShouldBe(VersionStatus.Initialized);
        versionStatus = await clientGrain.GetVersionStatusAsync(version2);
        versionStatus.ShouldBe(VersionStatus.Initialized);

        await clientGrain.StartAsync(version1);
        versionStatus = await clientGrain.GetVersionStatusAsync(version1);
        versionStatus.ShouldBe(VersionStatus.Started);

        await clientGrain.UpgradeVersionAsync();
        version = await clientGrain.GetVersionAsync();
        version.CurrentVersion.ShouldBe(version2);
        version.NewVersion.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateSubscriptionInfo_Test()
    {
        var clientId = "client-id01";
        var subscriptionInfo1 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "tDVV",
                FilterType = BlockFilterType.Transaction,
                OnlyConfirmedBlock = false,
                StartBlockNumber = 1009,
                SubscribeEvents = new List<FilterContractEventInput>()
                {
                    new FilterContractEventInput()
                    {
                        ContractAddress = "7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX",
                        EventNames = new List<string>(){"Transfer"}
                    }
                }
            }
        };
        
        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version1 = await clientGrain.AddSubscriptionInfoAsync(subscriptionInfo1);
        
        var subscription1 = await clientGrain.GetSubscriptionInfoAsync(version1);
        subscription1.Count.ShouldBe(1);
        subscription1[0].ChainId.ShouldBe("tDVV");
        subscription1[0].FilterType.ShouldBe(BlockFilterType.Transaction);
        subscription1[0].OnlyConfirmedBlock.ShouldBe(false);
        subscription1[0].StartBlockNumber.ShouldBe(1009);
        subscription1[0].SubscribeEvents.Count.ShouldBe(1);
        subscription1[0].SubscribeEvents[0].ContractAddress.ShouldBe("7RzVGiuVWkvL4VfVHdZfQF2Tri3sgLe9U991bohHFfSRZXuGX");
        subscription1[0].SubscribeEvents[0].EventNames.Count.ShouldBe(1);
        
        var subscriptionInfo2 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "tDVV",
                FilterType = BlockFilterType.Transaction,
                OnlyConfirmedBlock = false,
                StartBlockNumber = 1009,
                SubscribeEvents = new List<FilterContractEventInput>()
                {
                    new FilterContractEventInput()
                    {
                        ContractAddress = "UYdd84gLMsVdHrgkr3ogqe1ukhKwen8oj32Ks4J1dg6KH9PYC",
                        EventNames = new List<string>(){"Transfer","ManagerApproved"}
                    }
                }
            },
            new SubscriptionInfo()
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Block,
                OnlyConfirmedBlock = true,
                StartBlockNumber = 999
            }
        };
        await clientGrain.UpdateSubscriptionInfoAsync(version1, subscriptionInfo2);
        
        var subscription2 = await clientGrain.GetSubscriptionInfoAsync(version1);
        subscription2.Count.ShouldBe(2);
        subscription2[0].ChainId.ShouldBe("tDVV");
        subscription2[0].FilterType.ShouldBe(BlockFilterType.Transaction);
        subscription2[0].OnlyConfirmedBlock.ShouldBe(false);
        subscription2[0].StartBlockNumber.ShouldBe(1009);
        subscription2[0].SubscribeEvents.Count.ShouldBe(1);
        subscription2[0].SubscribeEvents[0].ContractAddress.ShouldBe("UYdd84gLMsVdHrgkr3ogqe1ukhKwen8oj32Ks4J1dg6KH9PYC");
        subscription2[0].SubscribeEvents[0].EventNames.Count.ShouldBe(2);
        subscription2[1].ChainId.ShouldBe("AELF");
        subscription2[1].FilterType.ShouldBe(BlockFilterType.Block);
        subscription2[1].OnlyConfirmedBlock.ShouldBe(true);
        subscription2[1].StartBlockNumber.ShouldBe(999);
    }

    [Fact]
    public async Task Stop_Test()
    {
        var clientId = "client";
        var subscriptionInfo1 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Block,
            }
        };
        
        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version1 = await clientGrain.AddSubscriptionInfoAsync(subscriptionInfo1);
        
        var subscription1 = await clientGrain.GetSubscriptionInfoAsync(version1);
        subscription1.Count.ShouldBe(1);
        subscription1[0].ChainId.ShouldBe("AELF");
        subscription1[0].FilterType.ShouldBe(BlockFilterType.Block);
        
        var subscriptionInfo2 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Transaction,
            }
        };
        
        var version2 = await clientGrain.AddSubscriptionInfoAsync(subscriptionInfo2);

        await clientGrain.StopAsync(version1);
        var version = await clientGrain.GetVersionAsync();
        version.CurrentVersion.ShouldBeNull();
        
        await clientGrain.StopAsync(version2);
        version = await clientGrain.GetVersionAsync();
        version.NewVersion.ShouldBeNull();
    }

    [Fact]
    public async Task RemoveVersionInfo_Test()
    {
        var clientId = "client";
        var subscriptionInfo1 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Block,
            }
        };
        
        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version1 = await clientGrain.AddSubscriptionInfoAsync(subscriptionInfo1);

        var subscriptionInfo2 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Transaction,
            }
        };
        
        var version2 = await clientGrain.AddSubscriptionInfoAsync(subscriptionInfo2);

        await clientGrain.AddBlockScanIdAsync(version1, "id1");
        await clientGrain.AddBlockScanIdAsync(version2, "id2");
        await clientGrain.AddBlockScanIdAsync("wrong-version", "id3");

        var ids = await clientGrain.GetBlockScanIdsAsync(version1);
        ids.Count.ShouldBe(1);
        ids[0].ShouldBe("id1");
        
        ids = await clientGrain.GetBlockScanIdsAsync(version2);
        ids.Count.ShouldBe(1);
        ids[0].ShouldBe("id2");

        await clientGrain.RemoveVersionInfoAsync(version1);
        ids = await clientGrain.GetBlockScanIdsAsync(version1);
        ids.Count.ShouldBe(1);
        
        await clientGrain.RemoveVersionInfoAsync(version2);
        ids = await clientGrain.GetBlockScanIdsAsync(version2);
        ids.Count.ShouldBe(0);
    }
}