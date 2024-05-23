using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Subscriptions;
using Orleans;
using Shouldly;
using Xunit;

namespace AeFinder.BlockScan;

public class BlockScanAppServiceTests : AeFinderApplicationOrleansTestBase
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
        var appId = "AppId";
        var chainId = "AELF";
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

        var version1 = await _blockScanAppService.AddSubscriptionAsync(appId, subscriptionInput, new byte[1]);

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
        
        var version2 = await _blockScanAppService.AddSubscriptionAsync(appId, subscriptionInput2, new byte[1]);
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
        
        var version3 = await _blockScanAppService.AddSubscriptionAsync(appId, subscriptionInfo3, new byte[1]);
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

    // [Fact]
    // public async Task UpdateSubscriptionTest()
    // {
    //     var clientId = "ClientTest";
    //     var subscriptionInfo1 = new List<SubscriptionInfo>
    //     {
    //         new SubscriptionInfo
    //         {
    //             ChainId = "AELF",
    //             FilterType = BlockFilterType.Transaction,
    //             OnlyConfirmedBlock = true,
    //             StartBlockNumber = 999,
    //             SubscribeEvents = new List<FilterContractEventInput>()
    //             {
    //                 new FilterContractEventInput()
    //                 {
    //                     ContractAddress = "23GxsoW9TRpLqX1Z5tjrmcRMMSn5bhtLAf4HtPj8JX9BerqTqp",
    //                     EventNames = new List<string>()
    //                     {
    //                         "Transfer"
    //                     }
    //                 }
    //             }
    //         }
    //     };
    //     var version1 = await _blockScanAppService.SubmitSubscriptionInfoAsync(clientId, subscriptionInfo1);
    //     
    //     var subscription = await _blockScanAppService.GetSubscriptionInfoAsync(clientId);
    //     subscription.CurrentVersion.Version.ShouldBe(version1);
    //     subscription.CurrentVersion.SubscriptionInfos[0].FilterType.ShouldBe(BlockFilterType.Transaction);
    //     subscription.NewVersion.ShouldBeNull();
    //     
    //     var subscriptionInfo2 = new List<SubscriptionInfo>
    //     {
    //         new SubscriptionInfo
    //         {
    //             ChainId = "AELF",
    //             FilterType = BlockFilterType.Transaction,
    //             OnlyConfirmedBlock = true,
    //             StartBlockNumber = 999,
    //             SubscribeEvents = new List<FilterContractEventInput>()
    //             {
    //                 new FilterContractEventInput()
    //                 {
    //                     ContractAddress = "23GxsoW9TRpLqX1Z5tjrmcRMMSn5bhtLAf4HtPj8JX9BerqTqp",
    //                     EventNames = new List<string>()
    //                     {
    //                         "Transfer",
    //                         "SetNumbered"
    //                     }
    //                 }
    //             }
    //         }
    //     };
    //     await _blockScanAppService.UpdateSubscriptionInfoAsync(clientId, version1, subscriptionInfo2);
    //     var subscription2 = await _blockScanAppService.GetSubscriptionInfoAsync(clientId);
    //     subscription2.CurrentVersion.Version.ShouldBe(version1);
    //     subscription2.CurrentVersion.SubscriptionInfos[0].FilterType.ShouldBe(BlockFilterType.Transaction);
    //     subscription2.CurrentVersion.SubscriptionInfos[0].SubscribeEvents.Count.ShouldBe(1);
    //     subscription2.CurrentVersion.SubscriptionInfos[0].SubscribeEvents[0].EventNames.Count.ShouldBe(2);
    // }
}