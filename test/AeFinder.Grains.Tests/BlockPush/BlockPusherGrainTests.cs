using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Chains;
using AeFinder.Grains.Grain.Subscriptions;
using Orleans;
using Orleans.Streams;
using Shouldly;
using Xunit;

namespace AeFinder.Grains.BlockPush;

[Collection(ClusterCollection.Name)]
public class BlockPusherGrainTests : AeFinderGrainTestBase
{
    private readonly IBlockDataProvider _blockDataProvider;

    public BlockPusherGrainTests()
    {
        _blockDataProvider = GetRequiredService<IBlockDataProvider>();
    }

    [Fact]
    public async Task Push_Test()
    {
        var chainId = "AELF";
        var appId = "DApp";
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash, _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash, _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = false,
                    StartBlockNumber = 21
                }
            }
        };
        var version = (await appGrain.AddSubscriptionAsync(subscription, new byte[] { })).NewVersion;
        
        var pushToken = Guid.NewGuid().ToString("N");
        await appGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, chainId);

        var pusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await pusherInfoGrain.InitializeAsync(appId, version, subscription.SubscriptionItems[0], pushToken);

        var blockPusherGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockPusherGrain.InitializeAsync(pushToken, 21);
        var streamId = await pusherInfoGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(AeFinderApplicationConsts.MessageStreamNamespace, streamId);

        var subscribedBlock = new List<AppSubscribedBlockDto>();
        await stream.SubscribeAsync((v, t) =>
            {
                v.ChainId.ShouldBe(chainId);
                v.AppId.ShouldBe(appId);
                v.Version.ShouldBe(version);
                v.PushToken.ShouldBe(pushToken);
                subscribedBlock.AddRange(v.Blocks);
           return Task.CompletedTask;
        });

        await blockPusherGrain.HandleBlockAsync(new BlockWithTransactionDto());
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 1), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(0);

        await blockPusherGrain.HandleHistoricalBlockAsync();
        
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 50), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(50);
        subscribedBlock.Count(o => o.Confirmed).ShouldBe(25);

        var pushMode = await pusherInfoGrain.GetPushModeAsync();
        pushMode.ShouldBe(BlockPushMode.NewBlock);
        
        await blockPusherGrain.HandleHistoricalBlockAsync();
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 51), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(50);
        
        await blockPusherGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 55), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await blockPusherGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 56), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await blockPusherGrain.HandleBlockAsync(_blockDataProvider.Blocks[49].First());
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 56), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await blockPusherGrain.HandleBlockAsync(_blockDataProvider.Blocks[51].First());
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 56), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await blockPusherGrain.HandleBlockAsync(_blockDataProvider.Blocks[53].First());
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 61), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(61);
        subscribedBlock.Last().BlockHeight.ShouldBe(53);
    }

    [Fact]
    public async Task Push_WrongVersion_Test()
    {
        var chainId = "AELF";
        var appId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash,
            _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);

        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 21
                }
            }
        };
        var version = (await appGrain.AddSubscriptionAsync(subscription, new byte[] { })).NewVersion;
        
        var pushToken = Guid.NewGuid().ToString("N");
        await appGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, chainId);

        var pusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await pusherInfoGrain.InitializeAsync(appId, version, subscription.SubscriptionItems[0], pushToken);

        var blockPusherGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockPusherGrain.InitializeAsync(pushToken, 21);
        var streamId = await pusherInfoGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(AeFinderApplicationConsts.MessageStreamNamespace, streamId);

        var subscribedBlock = new List<AppSubscribedBlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.AppId.ShouldBe(appId);
            v.Version.ShouldBe(version);
            v.PushToken.ShouldBe(pushToken);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await blockPusherGrain.HandleBlockAsync(new BlockWithTransactionDto());
        
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 1), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(0);

        await appGrain.StopAsync(version);
        
        await blockPusherGrain.HandleHistoricalBlockAsync();

        subscribedBlock.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Push_MissingBlock_Test()
    {
        var chainId = "AELF";
        var appId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync("BlockHash1000", 1000);
        await chainGrain.SetLatestConfirmedBlockAsync("BlockHash1000", 1000);

        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = false,
                    StartBlockNumber = 200
                }
            }
        };
        var version = (await appGrain.AddSubscriptionAsync(subscription, new byte[] { })).NewVersion;

        var pushToken = Guid.NewGuid().ToString("N");
        await appGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, chainId);

        var pusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await pusherInfoGrain.InitializeAsync(appId, version, subscription.SubscriptionItems[0], pushToken);
        
        var blockPusherGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockPusherGrain.InitializeAsync(pushToken, 200);

        await Assert.ThrowsAsync<ApplicationException>(async () => await blockPusherGrain.HandleHistoricalBlockAsync());

        await blockPusherGrain.InitializeAsync(pushToken, 180);
        await pusherInfoGrain.SetNewBlockStartHeightAsync(180);
        
        await Assert.ThrowsAsync<ApplicationException>(async () => await blockPusherGrain.HandleBlockAsync(new BlockWithTransactionDto
        {
            BlockHash = "BlockHash200",
            BlockHeight = 200
        }));
        
        await Assert.ThrowsAsync<ApplicationException>(async () => await blockPusherGrain.HandleConfirmedBlockAsync(new BlockWithTransactionDto
        {
            BlockHash = "BlockHash200",
            BlockHeight = 200
        }));
    }

    [Fact]
    public async Task Push_OnlyConfirmedBlock_Test()
    {
        var chainId = "AELF";
        var appId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash,
            _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);

        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = true,
                    StartBlockNumber = 21
                }
            }
        };
        var version = (await appGrain.AddSubscriptionAsync(subscription, new byte[] { })).NewVersion;
        
        var pushToken = Guid.NewGuid().ToString("N");
        await appGrain.StartAsync(version);

        var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, chainId);

        var pusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await pusherInfoGrain.InitializeAsync(appId, version, subscription.SubscriptionItems[0], pushToken);

        var blockPusherGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockPusherGrain.InitializeAsync(pushToken, 21);
        var streamId = await pusherInfoGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(AeFinderApplicationConsts.MessageStreamNamespace, streamId);

        var subscribedBlock = new List<AppSubscribedBlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.AppId.ShouldBe(appId);
            v.Version.ShouldBe(version);
            v.PushToken.ShouldBe(pushToken);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await blockPusherGrain.HandleConfirmedBlockAsync(new BlockWithTransactionDto());
        
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 1), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(0);

        await blockPusherGrain.HandleHistoricalBlockAsync();
        
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 25), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(25);
        var number = 21;
        foreach (var block in subscribedBlock)
        {
            block.BlockHeight.ShouldBe(number);
            block.Confirmed.ShouldBe(true);
            number++;
        }
    }

    [Fact]
    public async Task Push_ConfirmedBlockReceiveFirst_Test()
    {
        var chainId = "AELF";
        var appId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash,
            _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = false,
                    StartBlockNumber = 21
                }
            }
        };
        var version = (await appGrain.AddSubscriptionAsync(subscription, new byte[] { })).NewVersion;
        
        var pushToken = Guid.NewGuid().ToString("N");
        await appGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, chainId);

        var pusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await pusherInfoGrain.InitializeAsync(appId, version, subscription.SubscriptionItems[0], pushToken);

        var blockPusherGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockPusherGrain.InitializeAsync(pushToken, 21);
        var streamId = await pusherInfoGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(AeFinderApplicationConsts.MessageStreamNamespace, streamId);

        var subscribedBlock = new List<AppSubscribedBlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.AppId.ShouldBe(appId);
            v.Version.ShouldBe(version);
            v.PushToken.ShouldBe(pushToken);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await blockPusherGrain.HandleHistoricalBlockAsync();
        
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 50), TimeSpan.FromSeconds(5));
        
        subscribedBlock.Count.ShouldBe(50);
        
        await blockPusherGrain.HandleConfirmedBlockAsync(_blockDataProvider.Blocks[46].First());
        
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 51), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(50);
    }

    [Fact]
    public async Task Push_WithFilter_Test()
    {
        var chainId = "AELF";
        var appId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash,
            _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = false,
                    StartBlockNumber = 21,
                    LogEventConditions = new List<LogEventCondition>
                    {
                        new()
                        {
                            ContractAddress = "ContractAddress0",
                            EventNames = new List<string> { "EventName30", "EventName50" }
                        }
                    }
                }
            }
        };
        var version = (await appGrain.AddSubscriptionAsync(subscription, new byte[] { })).NewVersion;
        await appGrain.StartAsync(version);

        var pushToken = Guid.NewGuid().ToString("N");
        await appGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, chainId);

        var pusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await pusherInfoGrain.InitializeAsync(appId, version, subscription.SubscriptionItems[0], pushToken);

        var blockPusherGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockPusherGrain.InitializeAsync(pushToken, 21);
        var streamId = await pusherInfoGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(AeFinderApplicationConsts.MessageStreamNamespace, streamId);

        var subscribedBlock = new List<AppSubscribedBlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.AppId.ShouldBe(appId);
            v.Version.ShouldBe(version);
            v.PushToken.ShouldBe(pushToken);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await blockPusherGrain.HandleHistoricalBlockAsync();
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 50), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(50);
        subscribedBlock.Last().BlockHeight.ShouldBe(45);

        await blockPusherGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 55), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);

        await blockPusherGrain.HandleConfirmedBlockAsync( _blockDataProvider.Blocks[50].First() );
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 56), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
    }

    [Fact]
    public async Task BlockPushThreshold_Test()
    {
        var chainId = "AELF";
        var appId = "DApp";
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash, _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash, _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var appGrain = Cluster.Client.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = new SubscriptionManifest
        {
            SubscriptionItems = new List<Subscription>()
            {
                new()
                {
                    ChainId = chainId,
                    OnlyConfirmed = false,
                    StartBlockNumber = 1
                }
            }
        };
        var version = (await appGrain.AddSubscriptionAsync(subscription, new byte[] { })).NewVersion;

        var pushToken = Guid.NewGuid().ToString("N");
        await appGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, chainId);

        var pusherInfoGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await pusherInfoGrain.InitializeAsync(appId, version, subscription.SubscriptionItems[0], pushToken);

        var blockPusherGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockPusherGrain.InitializeAsync(pushToken, 1);
        var streamId = await pusherInfoGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(AeFinderApplicationConsts.MessageStreamNamespace, streamId);

        var subscribedBlock = new List<AppSubscribedBlockDto>();
        await stream.SubscribeAsync((v, t) =>
            {
                v.ChainId.ShouldBe(chainId);
                v.AppId.ShouldBe(appId);
                v.Version.ShouldBe(version);
                v.PushToken.ShouldBe(pushToken);
                subscribedBlock.AddRange(v.Blocks);
           return Task.CompletedTask;
        });

        var blockStateSetInfoGrain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, version, chainId));
        await blockStateSetInfoGrain.SetBlockStateSetStatusAsync(new BlockStateSetStatus
        {
            LastIrreversibleBlockHeight = 0,
        });
        
        await blockPusherGrain.HandleHistoricalBlockAsync();
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 80), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(80);
        
        await blockStateSetInfoGrain.SetBlockStateSetStatusAsync(new BlockStateSetStatus
        {
            LastIrreversibleBlockHeight = 10,
        });
        
        await blockPusherGrain.HandleHistoricalBlockAsync();
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 90), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(90);
        
        await pusherInfoGrain.SetNewBlockStartHeightAsync(9);
        
        await blockPusherGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 91), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(90);
        
        await blockPusherGrain.HandleConfirmedBlockAsync(_blockDataProvider.Blocks[50].First());
        //Wait for asynchronous processing stream message to complete
        await WaitUntilAsync(_ => Task.FromResult(subscribedBlock.Count == 91), TimeSpan.FromSeconds(5));
        subscribedBlock.Count.ShouldBe(90);
    }
    
    private async Task WaitUntilAsync(Func<bool, Task<bool>> predicate, TimeSpan timeout, TimeSpan? delayOnFail = null)
    {
        delayOnFail ??= TimeSpan.FromSeconds(1);
        var keepGoing = new[] { true };
        async Task Loop()
        {
            bool passed;
            do
            {
                // need to wait a bit to before re-checking the condition.
                await Task.Delay(delayOnFail.Value);
                passed = await predicate(false);
            }
            while (!passed && keepGoing[0]);
            if (!passed)
            {
                await predicate(true);
            }
        }

        var task = Loop();
        try
        {
            await Task.WhenAny(task, Task.Delay(timeout));
        }
        finally
        {
            keepGoing[0] = false;
        }

        await task;
    }
}