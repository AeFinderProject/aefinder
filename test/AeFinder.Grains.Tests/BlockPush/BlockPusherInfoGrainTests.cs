using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Subscriptions;
using Shouldly;
using Xunit;

namespace AeFinder.Grains.BlockPush;

[Collection(ClusterCollection.Name)]
public class BlockPusherInfoGrainTests : AeFinderGrainTestBase
{
     [Fact(Skip ="skip for timeout")]
    public async Task Initialize_Test()
    {
        var chainId = "AELF";
        var appId = "AppId";
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
        
        var pushToken = Guid.NewGuid().ToString("N");
        var blockPusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(appId);
        await blockPusherInfoGrain.InitializeAsync(appId, version, subscription.SubscriptionItems[0], pushToken);
        var pushInfo = await blockPusherInfoGrain.GetPushInfoAsync();
        pushInfo.AppId.ShouldBe(appId);
        var pushMode = await blockPusherInfoGrain.GetPushModeAsync();
        pushMode.ShouldBe(BlockPushMode.HistoricalBlock);

        await blockPusherInfoGrain.SetHandleHistoricalBlockTimeAsync(DateTime.UtcNow);
        var isNeedRecover = await blockPusherInfoGrain.IsNeedRecoverAsync();
        isNeedRecover.ShouldBeFalse();
        
        await blockPusherInfoGrain.SetHandleHistoricalBlockTimeAsync(DateTime.UtcNow.AddMinutes(-10));
        isNeedRecover = await blockPusherInfoGrain.IsNeedRecoverAsync();
        isNeedRecover.ShouldBeTrue();

        var subscribe = await blockPusherInfoGrain.GetSubscriptionAsync();
        subscribe.ChainId.ShouldBe(subscription.SubscriptionItems[0].ChainId);
        subscribe.OnlyConfirmed.ShouldBe(subscription.SubscriptionItems[0].OnlyConfirmed);
        subscribe.StartBlockNumber.ShouldBe(subscription.SubscriptionItems[0].StartBlockNumber);
        subscribe.LogEventConditions.Count.ShouldBe(1);
        subscribe.LogEventConditions[0].ContractAddress.ShouldBe(subscription.SubscriptionItems[0].LogEventConditions[0].ContractAddress);
        subscribe.LogEventConditions[0].EventNames.Count.ShouldBe(1);
        subscribe.LogEventConditions[0].EventNames[0].ShouldBe(subscription.SubscriptionItems[0].LogEventConditions[0].EventNames[0]);

        var pusherManagerGrain = Cluster.Client.GetGrain<IBlockPusherManagerGrain>(0);
        var ids = await pusherManagerGrain.GetBlockPusherIdsByChainAsync("AELF");
        ids.Count.ShouldBe(1);
        ids[0].ShouldBe(appId);

        var allBlockPusherIds = await pusherManagerGrain.GetAllBlockPusherIdsAsync();
        allBlockPusherIds.Keys.Count.ShouldBe(1);
        allBlockPusherIds["AELF"].Count.ShouldBe(1);
        allBlockPusherIds["AELF"].First().ShouldBe(appId);

        await blockPusherInfoGrain.SetNewBlockStartHeightAsync(80);
        pushMode = await blockPusherInfoGrain.GetPushModeAsync();
        pushMode.ShouldBe(BlockPushMode.NewBlock);
        
        isNeedRecover = await blockPusherInfoGrain.IsNeedRecoverAsync();
        isNeedRecover.ShouldBeFalse();

        ids = await pusherManagerGrain.GetBlockPusherIdsByChainAsync("AELF");
        ids.Count.ShouldBe(1);
        ids[0].ShouldBe(appId);
        
        allBlockPusherIds = await pusherManagerGrain.GetAllBlockPusherIdsAsync();
        allBlockPusherIds.Keys.Count.ShouldBe(1);
        allBlockPusherIds["AELF"].Count.ShouldBe(1);
        allBlockPusherIds["AELF"].First().ShouldBe(appId);
        
        await blockPusherInfoGrain.StopAsync();

        ids = await pusherManagerGrain.GetBlockPusherIdsByChainAsync("AELF");
        ids.Count.ShouldBe(0);
        
        allBlockPusherIds = await pusherManagerGrain.GetAllBlockPusherIdsAsync();
        allBlockPusherIds.Keys.Count.ShouldBe(1);
        allBlockPusherIds["AELF"].Count.ShouldBe(0);
        
        ids = await pusherManagerGrain.GetBlockPusherIdsByChainAsync("tDVV");
        ids.Count.ShouldBe(0);
    }
}