using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.BlockScanExecution;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.Grain.ScanApps;
using AElfIndexer.Grains.State.ScanApps;
using AElfIndexer.Grains.State.Subscriptions;
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
        var chainId = "AELF";
        var subscriptionInput = new SubscriptionDto()
        {
            SubscriptionItems = new List<SubscriptionItemDto>()
            {
                new()
                {
                    ChainId = chainId,
                    StartBlockNumber = 100,
                    OnlyConfirmed = true
                }
            }
        };

        var version1 = await _blockScanAppService.AddSubscriptionAsync(clientId, subscriptionInput);

        var subscription = await _blockScanAppService.GetSubscriptionAsync(clientId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.Subscription.SubscriptionItems[0].StartBlockNumber.ShouldBe(100);
        subscription.NewVersion.ShouldBeNull();
        
        await _blockScanAppService.UpgradeVersionAsync(clientId);
        
        subscription = await _blockScanAppService.GetSubscriptionAsync(clientId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.NewVersion.ShouldBeNull();
        
        var streamIds = await _blockScanAppService.GetMessageStreamIdsAsync(clientId, version1);
        var id1 = GrainIdHelper.GenerateBlockScanGrainId(clientId, version1, chainId);
        var blockScanGrain = _clusterClient.GetGrain<IBlockScanGrain>(id1);
        var streamId = await blockScanGrain.GetMessageStreamIdAsync();
        streamIds.Count.ShouldBe(1);
        streamIds[0].ShouldBe(streamId);

        await _blockScanAppService.StartScanAsync(clientId, version1);
        
        var blockScanManagerGrain = _clusterClient.GetGrain<IBlockScanManagerGrain>(GrainIdHelper.GenerateBlockScanManagerGrainId());
        var scanIds = await blockScanManagerGrain.GetAllBlockScanIdsAsync();
        scanIds[chainId].Count.ShouldBe(1);
        scanIds[chainId].ShouldContain(id1);

        var scanAppGrain = _clusterClient.GetGrain<IScanAppGrain>(GrainIdHelper.GenerateScanAppGrainId(clientId));
        var versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Started);
        
        var token = await blockScanGrain.GetScanTokenAsync();
        var isRunning = await _blockScanAppService.IsRunningAsync(chainId, clientId, version1, token);
        isRunning.ShouldBeTrue();

        var subscriptionInput2 = new SubscriptionDto()
        {
            SubscriptionItems = new List<SubscriptionItemDto>()
            {
                new()
                {
                    ChainId = chainId,
                    StartBlockNumber = 200
                }
            }
        };
        
        var version2 = await _blockScanAppService.AddSubscriptionAsync(clientId, subscriptionInput2);
        var id2 = GrainIdHelper.GenerateBlockScanGrainId(clientId, version2, chainId);
        
        subscription = await _blockScanAppService.GetSubscriptionAsync(clientId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.Subscription.SubscriptionItems[0].StartBlockNumber.ShouldBe(100);
        subscription.NewVersion.Version.ShouldBe(version2);
        subscription.NewVersion.Subscription.SubscriptionItems[0].StartBlockNumber.ShouldBe(200);
        
        await _blockScanAppService.PauseAsync(clientId, version1);
        scanIds = await blockScanManagerGrain.GetAllBlockScanIdsAsync();
        scanIds[chainId].Count.ShouldBe(0);

        versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Paused);
        
        token = await blockScanGrain.GetScanTokenAsync();
        isRunning = await _blockScanAppService.IsRunningAsync(chainId,clientId, version1, token);
        isRunning.ShouldBeFalse();
        
        await _blockScanAppService.StartScanAsync(clientId, version1);
        
        versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Started);
        
        token = await blockScanGrain.GetScanTokenAsync();
        isRunning = await _blockScanAppService.IsRunningAsync(chainId,clientId, version1, token);
        isRunning.ShouldBeTrue();

        await _blockScanAppService.StartScanAsync(clientId, version2);
        
        versionStatus = await scanAppGrain.GetSubscriptionStatusAsync(version1);
        versionStatus.ShouldBe(SubscriptionStatus.Started);
        
        var subscriptionInfo3 = new SubscriptionDto()
        {
            SubscriptionItems = new List<SubscriptionItemDto>()
            {
                new()
                {
                    ChainId = chainId,
                    StartBlockNumber = 300
                }
            }
        };
        
        var version3 = await _blockScanAppService.AddSubscriptionAsync(clientId, subscriptionInfo3);
        var id3 = GrainIdHelper.GenerateBlockScanGrainId(clientId, version3, chainId);

        subscription = await _blockScanAppService.GetSubscriptionAsync(clientId);
        subscription.CurrentVersion.Version.ShouldBe(version1);
        subscription.CurrentVersion.Subscription.SubscriptionItems[0].StartBlockNumber.ShouldBe(100);
        subscription.NewVersion.Version.ShouldBe(version3);
        subscription.NewVersion.Subscription.SubscriptionItems[0].StartBlockNumber.ShouldBe(300);
        
        await _blockScanAppService.StartScanAsync(clientId, version3);
        
        blockScanManagerGrain = _clusterClient.GetGrain<IBlockScanManagerGrain>(0);
        var allScanIds = await blockScanManagerGrain.GetAllBlockScanIdsAsync();
        allScanIds[chainId].ShouldNotContain(id2);
        allScanIds[chainId].ShouldContain(id3);

        await _blockScanAppService.UpgradeVersionAsync(clientId);
        
        subscription = await _blockScanAppService.GetSubscriptionAsync(clientId);
        subscription.CurrentVersion.Version.ShouldBe(version3);
        subscription.CurrentVersion.Subscription.SubscriptionItems[0].StartBlockNumber.ShouldBe(300);
        subscription.NewVersion.ShouldBeNull();
        
        allScanIds = await blockScanManagerGrain.GetAllBlockScanIdsAsync();
        allScanIds[chainId].ShouldNotContain(id1);
        
        await _blockScanAppService.StartScanAsync(clientId, version3);

        await _blockScanAppService.StopAsync(clientId, version3);

        subscription = await _blockScanAppService.GetSubscriptionAsync(clientId);
        subscription.CurrentVersion.ShouldBeNull();
        subscription.NewVersion.ShouldBeNull();
        
        allScanIds = await blockScanManagerGrain.GetAllBlockScanIdsAsync();
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