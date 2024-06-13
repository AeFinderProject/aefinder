using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Subscriptions;
using Orleans;
using Shouldly;
using Xunit;

namespace AeFinder.BlockScan;

public class BlockScanAppServiceTests : AeFinderApplicationOrleansTestBase
{
    private readonly IBlockScanAppService _blockScanAppService;
    private ISubscriptionAppService _subscriptionAppService;
    private readonly IClusterClient _clusterClient;
    private readonly IAppService _appService;

    public BlockScanAppServiceTests()
    {
        _subscriptionAppService = GetRequiredService<ISubscriptionAppService>();
        _blockScanAppService = GetRequiredService<IBlockScanAppService>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _appService = GetRequiredService<IAppService>();
    }

    [Fact]
    public async Task ScanTest()
    {
        var chainId = "AELF";
        var appId = (await _appService.CreateAsync(new CreateAppDto { AppName = "AppId" })).AppId;
        var subscriptionInput = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new()
                {
                    ChainId = chainId,
                    StartBlockNumber = 100,
                    OnlyConfirmed = true
                }
            }
        };

        var version1 = await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1]);

        var subscription = await _blockScanAppService.GetSubscriptionAsync(appId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(100);
        subscription.PendingVersion.ShouldBeNull();
        
        await _blockScanAppService.UpgradeVersionAsync(appId);
        
        subscription = await _blockScanAppService.GetSubscriptionAsync(appId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.PendingVersion.ShouldBeNull();
        
        var streamIds = await _blockScanAppService.GetMessageStreamIdsAsync(appId, version1);
        var id1 = GrainIdHelper.GenerateBlockPusherGrainId(appId, version1, chainId);
        var blockScanGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(id1);
        var streamId = await blockScanGrain.GetMessageStreamIdAsync();
        streamIds.Count.ShouldBe(1);
        streamIds[0].ShouldBe(streamId);

        await _blockScanAppService.StartScanAsync(appId, version1);
        
        var blockScanManagerGrain = _clusterClient.GetGrain<IBlockPusherManagerGrain>(GrainIdHelper.GenerateBlockPusherManagerGrainId());
        var scanIds = await blockScanManagerGrain.GetAllBlockPusherIdsAsync();
        scanIds[chainId].Count.ShouldBe(1);
        scanIds[chainId].ShouldContain(id1);

        var scanAppGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Started);
        
        var token = await blockScanGrain.GetPushTokenAsync();
        var isRunning = await _blockScanAppService.IsRunningAsync(chainId, appId, version1, token);
        isRunning.ShouldBeTrue();

        var subscriptionInput2 = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new()
                {
                    ChainId = chainId,
                    StartBlockNumber = 200
                }
            }
        };
        
        var version2 = await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInput2, new byte[1]);
        var id2 = GrainIdHelper.GenerateBlockPusherGrainId(appId, version2, chainId);
        
        subscription = await _blockScanAppService.GetSubscriptionAsync(appId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(100);
        subscription.PendingVersion.Version.ShouldBe(version2);
        subscription.PendingVersion.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(200);
        
        await _blockScanAppService.PauseAsync(appId, version1);
        scanIds = await blockScanManagerGrain.GetAllBlockPusherIdsAsync();
        scanIds[chainId].Count.ShouldBe(0);

        versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Paused);
        
        token = await blockScanGrain.GetPushTokenAsync();
        isRunning = await _blockScanAppService.IsRunningAsync(chainId,appId, version1, token);
        isRunning.ShouldBeFalse();
        
        await _blockScanAppService.StartScanAsync(appId, version1);
        
        versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Started);
        
        token = await blockScanGrain.GetPushTokenAsync();
        isRunning = await _blockScanAppService.IsRunningAsync(chainId,appId, version1, token);
        isRunning.ShouldBeTrue();

        await _blockScanAppService.StartScanAsync(appId, version2);
        
        versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Started);
        
        var subscriptionInfo3 = new SubscriptionManifestDto()
        {
            SubscriptionItems = new List<SubscriptionDto>()
            {
                new()
                {
                    ChainId = chainId,
                    StartBlockNumber = 300
                }
            }
        };
        
        var version3 = await _subscriptionAppService.AddSubscriptionAsync(appId, subscriptionInfo3, new byte[1]);
        var id3 = GrainIdHelper.GenerateBlockPusherGrainId(appId, version3, chainId);

        subscription = await _blockScanAppService.GetSubscriptionAsync(appId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(100);
        subscription.PendingVersion.Version.ShouldBe(version3);
        subscription.PendingVersion.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(300);
        
        await _blockScanAppService.StartScanAsync(appId, version3);
        
        blockScanManagerGrain = _clusterClient.GetGrain<IBlockPusherManagerGrain>(0);
        var allScanIds = await blockScanManagerGrain.GetAllBlockPusherIdsAsync();
        allScanIds[chainId].ShouldNotContain(id2);
        allScanIds[chainId].ShouldContain(id3);

        await _blockScanAppService.UpgradeVersionAsync(appId);
        
        subscription = await _blockScanAppService.GetSubscriptionAsync(appId);
        subscription.CurrentVersion.Version.ShouldBe(version3);
        subscription.CurrentVersion.SubscriptionManifest.SubscriptionItems[0].StartBlockNumber.ShouldBe(300);
        subscription.PendingVersion.ShouldBeNull();
        
        allScanIds = await blockScanManagerGrain.GetAllBlockPusherIdsAsync();
        allScanIds[chainId].ShouldNotContain(id1);
        
        await _blockScanAppService.StartScanAsync(appId, version3);

        await _blockScanAppService.StopAsync(appId, version3);

        subscription = await _blockScanAppService.GetSubscriptionAsync(appId);
        subscription.CurrentVersion.ShouldBeNull();
        subscription.PendingVersion.ShouldBeNull();
        
        allScanIds = await blockScanManagerGrain.GetAllBlockPusherIdsAsync();
        allScanIds[chainId].ShouldNotContain(id3);
    }
}