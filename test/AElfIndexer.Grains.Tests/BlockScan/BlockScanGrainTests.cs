using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.BlockPush;
using AElfIndexer.Grains.State.Subscriptions;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.BlockScan;

[Collection(ClusterCollection.Name)]
public class BlockScanGrainTests : AElfIndexerGrainTestBase
{
    [Fact]
    public async Task Initialize_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        var version = "Version";
        var subscription = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 100,
                    LogEventConditions = new List<LogEventCondition>
                    {
                        new()
                        {
                            ContractAddress = "ContractAddress",
                            EventNames = new List<string> { "EventName" }
                        }
                    }

                }
            }
        };
        
        var scanToken = Guid.NewGuid().ToString("N");
        var blockScanGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(clientId);
        await blockScanGrain.InitializeAsync(clientId, version, subscription.SubscriptionItems[0], scanToken);
        var clientInfo = await blockScanGrain.GetPushInfoAsync();
        clientInfo.AppId.ShouldBe(clientId);
        var scanMode = await blockScanGrain.GetPushModeAsync();
        scanMode.ShouldBe(BlockPushMode.HistoricalBlock);

        var subscribe = await blockScanGrain.GetSubscriptionAsync();
        subscribe.ChainId.ShouldBe(subscription.SubscriptionItems[0].ChainId);
        subscribe.OnlyConfirmed.ShouldBe(subscription.SubscriptionItems[0].OnlyConfirmed);
        subscribe.StartBlockNumber.ShouldBe(subscription.SubscriptionItems[0].StartBlockNumber);
        subscribe.LogEventConditions.Count.ShouldBe(1);
        subscribe.LogEventConditions[0].ContractAddress.ShouldBe(subscription.SubscriptionItems[0].LogEventConditions[0].ContractAddress);
        subscribe.LogEventConditions[0].EventNames.Count.ShouldBe(1);
        subscribe.LogEventConditions[0].EventNames[0].ShouldBe(subscription.SubscriptionItems[0].LogEventConditions[0].EventNames[0]);

        var clientManagerGrain = Cluster.Client.GetGrain<IBlockPusherManagerGrain>(0);
        var clientIds = await clientManagerGrain.GetBlockPusherIdsByChainAsync("AELF");
        clientIds.Count.ShouldBe(1);
        clientIds[0].ShouldBe(clientId);

        var allClientIds = await clientManagerGrain.GetAllBlockPusherIdsAsync();
        allClientIds.Keys.Count.ShouldBe(1);
        allClientIds["AELF"].Count.ShouldBe(1);
        allClientIds["AELF"].First().ShouldBe(clientId);

        await blockScanGrain.SetNewBlockStartHeightAsync(80);
        scanMode = await blockScanGrain.GetPushModeAsync();
        scanMode.ShouldBe(BlockPushMode.NewBlock);

        clientIds = await clientManagerGrain.GetBlockPusherIdsByChainAsync("AELF");
        clientIds.Count.ShouldBe(1);
        clientIds[0].ShouldBe(clientId);
        
        allClientIds = await clientManagerGrain.GetAllBlockPusherIdsAsync();
        allClientIds.Keys.Count.ShouldBe(1);
        allClientIds["AELF"].Count.ShouldBe(1);
        allClientIds["AELF"].First().ShouldBe(clientId);
        
        await blockScanGrain.StopAsync();

        clientIds = await clientManagerGrain.GetBlockPusherIdsByChainAsync("AELF");
        clientIds.Count.ShouldBe(0);
        
        allClientIds = await clientManagerGrain.GetAllBlockPusherIdsAsync();
        allClientIds.Keys.Count.ShouldBe(1);
        allClientIds["AELF"].Count.ShouldBe(0);
        
        clientIds = await clientManagerGrain.GetBlockPusherIdsByChainAsync("tDVV");
        clientIds.Count.ShouldBe(0);
    }
}