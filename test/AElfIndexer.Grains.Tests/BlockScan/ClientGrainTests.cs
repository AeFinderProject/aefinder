using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.BlockScan;

[Collection(ClusterCollection.Name)]
public class ClientGrainTests : AElfIndexerGrainTestBase
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
        await clientGrain.SetTokenAsync(version1);
        var token1 = await clientGrain.GetTokenAsync(version1);
        await clientGrain.SetTokenAsync(version2);
        var token2 = await clientGrain.GetTokenAsync(version2);

        var subscription2 = await clientGrain.GetSubscriptionInfoAsync(version2);
        subscription2.Count.ShouldBe(1);
        subscription2[0].ChainId.ShouldBe("AELF");
        subscription2[0].FilterType.ShouldBe(BlockFilterType.Transaction);
        
        var isAvailable = await clientGrain.IsRunningAsync(version1, token1);
        isAvailable.ShouldBeFalse();
        isAvailable = await clientGrain.IsRunningAsync(version2, token2);
        isAvailable.ShouldBeFalse();

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
        
        isAvailable = await clientGrain.IsRunningAsync(version1, token1);
        isAvailable.ShouldBeTrue();
        isAvailable = await clientGrain.IsRunningAsync(Guid.NewGuid().ToString(), token1);
        isAvailable.ShouldBeFalse();
        isAvailable = await clientGrain.IsRunningAsync(null, token1);
        isAvailable.ShouldBeFalse();
        isAvailable = await clientGrain.IsRunningAsync(string.Empty, token1);
        isAvailable.ShouldBeFalse();
        isAvailable = await clientGrain.IsRunningAsync(version1, token2);
        isAvailable.ShouldBeFalse();
        
        isAvailable = await clientGrain.IsRunningAsync(version2, token2);
        isAvailable.ShouldBeFalse();

        await clientGrain.UpgradeVersionAsync();
        version = await clientGrain.GetVersionAsync();
        version.CurrentVersion.ShouldBe(version2);
        version.NewVersion.ShouldBeNull();
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