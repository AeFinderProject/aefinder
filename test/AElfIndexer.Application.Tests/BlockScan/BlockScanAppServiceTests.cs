using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.BlockScan;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Shouldly;
using Xunit;

namespace AElfIndexer.BlockScan;

public class BlockScanAppServiceTests : AElfIndexerApplicationOrleansTestBase
{
    private IBlockScanAppService _blockScanAppService;
    private IClusterClient _clusterClient;

    public BlockScanAppServiceTests()
    {
        _blockScanAppService = GetRequiredService<IBlockScanAppService>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }

    [Fact]
    public async Task ScanTest()
    {
        var clientId = "Client";
        var subscriptionInfo1 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Block
            }
        };

        var version1 = await _blockScanAppService.SubmitSubscriptionInfoAsync(clientId, subscriptionInfo1);

        var subscription = await _blockScanAppService.GetSubscriptionInfoAsync(clientId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.SubscriptionInfos[0].FilterType.ShouldBe(BlockFilterType.Block);
        subscription.NewVersion.ShouldBeNull();

        var version = await _blockScanAppService.GetClientVersionAsync(clientId);
        version.CurrentVersion.ShouldBe(version1);
        version.NewVersion.ShouldBeNull();
        
        await _blockScanAppService.UpgradeVersionAsync(clientId);
        
        version = await _blockScanAppService.GetClientVersionAsync(clientId);
        version.CurrentVersion.ShouldBe(version1);
        version.NewVersion.ShouldBeNull();
        
        var streamIds = await _blockScanAppService.GetMessageStreamIdsAsync(clientId, version1);
        var id = GrainIdHelper.GenerateGrainId(subscriptionInfo1[0].ChainId, clientId, version1,
            subscriptionInfo1[0].FilterType);
        var blockScanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(id);
        var streamId = await blockScanInfoGrain.GetMessageStreamIdAsync();
        streamIds.Count.ShouldBe(1);
        streamIds[0].ShouldBe(streamId);

        await _blockScanAppService.StartScanAsync(clientId, version1);
        
        var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
        var scanIds = await clientGrain.GetBlockScanIdsAsync(version1);
        scanIds.Count.ShouldBe(1);
        scanIds[0].ShouldBe(id);

        var versionStatus = await clientGrain.GetVersionStatusAsync(version1);
        versionStatus.ShouldBe(VersionStatus.Started);
        var token = await _blockScanAppService.GetClientTokenAsync(clientId, version1);
        var isRunning = await _blockScanAppService.IsVersionRunningAsync(clientId, version1, token);
        isRunning.ShouldBeTrue();

        var subscriptionInfo2 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.Transaction
            }
        };
        
        var version2 = await _blockScanAppService.SubmitSubscriptionInfoAsync(clientId, subscriptionInfo2);

        subscription = await _blockScanAppService.GetSubscriptionInfoAsync(clientId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.SubscriptionInfos[0].FilterType.ShouldBe(BlockFilterType.Block);
        subscription.NewVersion.Version.ShouldBe(version2);
        subscription.NewVersion.SubscriptionInfos[0].FilterType.ShouldBe(BlockFilterType.Transaction);
        
        version = await _blockScanAppService.GetClientVersionAsync(clientId);
        version.CurrentVersion.ShouldBe(version1);
        version.NewVersion.ShouldBe(version2);
        
        await _blockScanAppService.PauseAsync(clientId, version1);
        scanIds = await clientGrain.GetBlockScanIdsAsync(version1);
        scanIds.Count.ShouldBe(1);
        scanIds[0].ShouldBe(id);

        versionStatus = await clientGrain.GetVersionStatusAsync(version1);
        versionStatus.ShouldBe(VersionStatus.Paused);
        
        token = await _blockScanAppService.GetClientTokenAsync(clientId, version1);
        isRunning = await _blockScanAppService.IsVersionRunningAsync(clientId, version1, token);
        isRunning.ShouldBeFalse();
        
        await _blockScanAppService.StartScanAsync(clientId, version1);
        
        versionStatus = await clientGrain.GetVersionStatusAsync(version1);
        versionStatus.ShouldBe(VersionStatus.Started);
        
        token = await _blockScanAppService.GetClientTokenAsync(clientId, version1);
        isRunning = await _blockScanAppService.IsVersionRunningAsync(clientId, version1, token);
        isRunning.ShouldBeTrue();

        await _blockScanAppService.StartScanAsync(clientId, version2);
        
        versionStatus = await clientGrain.GetVersionStatusAsync(version1);
        versionStatus.ShouldBe(VersionStatus.Started);
        
        var subscriptionInfo3 = new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = "AELF",
                FilterType = BlockFilterType.LogEvent
            }
        };
        
        var version3 = await _blockScanAppService.SubmitSubscriptionInfoAsync(clientId, subscriptionInfo3);

        subscription = await _blockScanAppService.GetSubscriptionInfoAsync(clientId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.SubscriptionInfos[0].FilterType.ShouldBe(BlockFilterType.Block);
        subscription.NewVersion.Version.ShouldBe(version3);
        subscription.NewVersion.SubscriptionInfos[0].FilterType.ShouldBe(BlockFilterType.LogEvent);
        
        version = await _blockScanAppService.GetClientVersionAsync(clientId);
        version.CurrentVersion.ShouldBe(version1);
        version.NewVersion.ShouldBe(version3);
        
        await _blockScanAppService.StartScanAsync(clientId, version3);
        
        var blockScanManagerGrain = _clusterClient.GetGrain<IBlockScanManagerGrain>(0);
        var allScanIds = await blockScanManagerGrain.GetAllBlockScanIdsAsync();
        allScanIds["AELF"].ShouldNotContain(subscriptionInfo2[0].ChainId + clientId + version2 + subscriptionInfo2[0].FilterType);

        scanIds = await clientGrain.GetBlockScanIdsAsync(version2);
        scanIds.Count.ShouldBe(0);

        await _blockScanAppService.UpgradeVersionAsync(clientId);
        
        version = await _blockScanAppService.GetClientVersionAsync(clientId);
        version.CurrentVersion.ShouldBe(version3);
        version.NewVersion.ShouldBeNull();
        
        allScanIds = await blockScanManagerGrain.GetAllBlockScanIdsAsync();
        allScanIds["AELF"].ShouldNotContain(subscriptionInfo1[0].ChainId + clientId + version1 + subscriptionInfo1[0].FilterType);
        
        await _blockScanAppService.StartScanAsync(clientId, version3);

        await _blockScanAppService.StopAsync(clientId, version3);
        
        version = await _blockScanAppService.GetClientVersionAsync(clientId);
        version.CurrentVersion.ShouldBeNull();
        version.NewVersion.ShouldBeNull();
        
        allScanIds = await blockScanManagerGrain.GetAllBlockScanIdsAsync();
        allScanIds["AELF"].ShouldNotContain(subscriptionInfo3[0].ChainId + clientId + version3 + subscriptionInfo3[0].FilterType);
    }
}