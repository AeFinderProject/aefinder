using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.Apps;
using AElfIndexer.Grains.Grain.BlockPush;
using AElfIndexer.Grains.Grain.BlockStates;
using AElfIndexer.Grains.Grain.Chains;
using AElfIndexer.Grains.State.Subscriptions;
using Orleans.Streams;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.BlockScan;

[Collection(ClusterCollection.Name)]
public class BlockScanExecutorGrainTests : AElfIndexerGrainTestBase
{
    private readonly IBlockDataProvider _blockDataProvider;

    public BlockScanExecutorGrainTests()
    {
        _blockDataProvider = GetRequiredService<IBlockDataProvider>();
    }

    [Fact]
    public async Task BlockTest()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash, _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash, _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var scanAppGrain = Cluster.Client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(clientId));
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
        var version = await scanAppGrain.AddSubscriptionAsync(subscription);
        
        var scanToken = Guid.NewGuid().ToString("N");
        await scanAppGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, chainId);

        var blockScanGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await blockScanGrain.InitializeAsync(clientId, version, subscription.SubscriptionItems[0], scanToken);

        var blockScanExecutorGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockScanExecutorGrain.InitializeAsync(scanToken, 21);
        var streamId = await blockScanGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
            {
                v.ChainId.ShouldBe(chainId);
                v.AppId.ShouldBe(clientId);
                v.Version.ShouldBe(version);
                v.PushToken.ShouldBe(scanToken);
                subscribedBlock.AddRange(v.Blocks);
           return Task.CompletedTask;
        });

        await blockScanExecutorGrain.HandleBlockAsync(new BlockWithTransactionDto());
        subscribedBlock.Count.ShouldBe(0);

        await blockScanExecutorGrain.HandleHistoricalBlockAsync();
        
        subscribedBlock.Count.ShouldBe(50);
        subscribedBlock.Count(o => o.Confirmed).ShouldBe(25);

        var scanMode = await blockScanGrain.GetPushModeAsync();
        scanMode.ShouldBe(BlockPushMode.NewBlock);
        
        await blockScanExecutorGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(50);
        
        await blockScanExecutorGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await blockScanExecutorGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await blockScanExecutorGrain.HandleBlockAsync(_blockDataProvider.Blocks[49].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await blockScanExecutorGrain.HandleBlockAsync(_blockDataProvider.Blocks[51].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await blockScanExecutorGrain.HandleBlockAsync(_blockDataProvider.Blocks[53].First());
        subscribedBlock.Count.ShouldBe(61);
        subscribedBlock.Last().BlockHeight.ShouldBe(53);
    }

    [Fact]
    public async Task Block_WrongVersion_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash,
            _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);

        var clientGrain = Cluster.Client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(clientId));
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
        var version = await clientGrain.AddSubscriptionAsync(subscription);
        
        var scanToken = Guid.NewGuid().ToString("N");
        var scanAppGrain = Cluster.Client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(clientId));
        await scanAppGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, chainId);

        var blockScanGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await blockScanGrain.InitializeAsync(clientId, version, subscription.SubscriptionItems[0], scanToken);

        var blockScanExecutorGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockScanExecutorGrain.InitializeAsync(scanToken, 21);
        var streamId = await blockScanGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.AppId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            v.PushToken.ShouldBe(scanToken);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await blockScanExecutorGrain.HandleBlockAsync(new BlockWithTransactionDto());
        subscribedBlock.Count.ShouldBe(0);

        await clientGrain.StopAsync(version);
        
        await blockScanExecutorGrain.HandleHistoricalBlockAsync();

        subscribedBlock.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Block_MissingBlock_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync("BlockHash1000", 1000);
        await chainGrain.SetLatestConfirmedBlockAsync("BlockHash1000", 1000);

        var scanAppGrain = Cluster.Client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(clientId));
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
        var version = await scanAppGrain.AddSubscriptionAsync(subscription);

        var scanToken = Guid.NewGuid().ToString("N");
        await scanAppGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, chainId);

        var blockScanGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await blockScanGrain.InitializeAsync(clientId, version, subscription.SubscriptionItems[0], scanToken);
        
        var blockScanExecutorGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockScanExecutorGrain.InitializeAsync(scanToken, 200);

        await Assert.ThrowsAsync<ApplicationException>(async () => await blockScanExecutorGrain.HandleHistoricalBlockAsync());

        await blockScanExecutorGrain.InitializeAsync(scanToken, 180);
        await blockScanGrain.SetNewBlockStartHeightAsync(180);
        
        await Assert.ThrowsAsync<ApplicationException>(async () => await blockScanExecutorGrain.HandleBlockAsync(new BlockWithTransactionDto
        {
            BlockHash = "BlockHash200",
            BlockHeight = 200
        }));
        
        await Assert.ThrowsAsync<ApplicationException>(async () => await blockScanExecutorGrain.HandleConfirmedBlockAsync(new BlockWithTransactionDto
        {
            BlockHash = "BlockHash200",
            BlockHeight = 200
        }));
    }

    [Fact]
    public async Task OnlyConfirmedBlock_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash,
            _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);

        var scanAppGrain = Cluster.Client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(clientId));
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
        var version = await scanAppGrain.AddSubscriptionAsync(subscription);
        
        var scanToken = Guid.NewGuid().ToString("N");
        await scanAppGrain.StartAsync(version);

        var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, chainId);

        var blockScanGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await blockScanGrain.InitializeAsync(clientId, version, subscription.SubscriptionItems[0], scanToken);

        var blockScanExecutorGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockScanExecutorGrain.InitializeAsync(scanToken, 21);
        var streamId = await blockScanGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.AppId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            v.PushToken.ShouldBe(scanToken);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await blockScanExecutorGrain.HandleConfirmedBlockAsync(new BlockWithTransactionDto());
        subscribedBlock.Count.ShouldBe(0);

        await blockScanExecutorGrain.HandleHistoricalBlockAsync();
        
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
    public async Task ConfirmedBlockReceiveFirst_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash,
            _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var scanAppGrain = Cluster.Client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(clientId));
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
        var version = await scanAppGrain.AddSubscriptionAsync(subscription);
        
        var scanToken = Guid.NewGuid().ToString("N");
        await scanAppGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, chainId);

        var blockScanGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await blockScanGrain.InitializeAsync(clientId, version, subscription.SubscriptionItems[0], scanToken);

        var scanGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await scanGrain.InitializeAsync(scanToken, 21);
        var streamId = await blockScanGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.AppId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            v.PushToken.ShouldBe(scanToken);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });
        
        await scanGrain.HandleHistoricalBlockAsync();
        
        subscribedBlock.Count.ShouldBe(50);
        
        await scanGrain.HandleConfirmedBlockAsync(_blockDataProvider.Blocks[46].First());
        subscribedBlock.Count.ShouldBe(50);
    }

    [Fact]
    public async Task Block_WithFilter_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash,
            _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var scanAppGrain = Cluster.Client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(clientId));
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
        var version = await scanAppGrain.AddSubscriptionAsync(subscription);
        await scanAppGrain.StartAsync(version);

        var scanToken = Guid.NewGuid().ToString("N");
        await scanAppGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, chainId);

        var blockScanGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await blockScanGrain.InitializeAsync(clientId, version, subscription.SubscriptionItems[0], scanToken);

        var blockScanExecutorGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockScanExecutorGrain.InitializeAsync(scanToken, 21);
        var streamId = await blockScanGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockWithTransactionDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.AppId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            v.PushToken.ShouldBe(scanToken);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await blockScanExecutorGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(50);
        subscribedBlock.Last().BlockHeight.ShouldBe(45);

        await blockScanExecutorGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);

        await blockScanExecutorGrain.HandleConfirmedBlockAsync( _blockDataProvider.Blocks[50].First() );
        subscribedBlock.Count.ShouldBe(60);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
    }

    [Fact]
    public async Task Block_BlockPushThreshold_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash, _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmedBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash, _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var scanAppGrain = Cluster.Client.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(clientId));
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
        var version = await scanAppGrain.AddSubscriptionAsync(subscription);

        var scanToken = Guid.NewGuid().ToString("N");
        await scanAppGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, chainId);

        var blockScanGrain = Cluster.Client.GetGrain<IBlockPusherInfoGrain>(id);
        await blockScanGrain.InitializeAsync(clientId, version, subscription.SubscriptionItems[0], scanToken);

        var blockScanExecutorGrain = Cluster.Client.GetGrain<IBlockPusherGrain>(id);
        await blockScanExecutorGrain.InitializeAsync(scanToken, 1);
        var streamId = await blockScanGrain.GetMessageStreamIdAsync();
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
            {
                v.ChainId.ShouldBe(chainId);
                v.AppId.ShouldBe(clientId);
                v.Version.ShouldBe(version);
                v.PushToken.ShouldBe(scanToken);
                subscribedBlock.AddRange(v.Blocks);
           return Task.CompletedTask;
        });
        
        var blockStateSetInfoGrain = Cluster.Client.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateGrainId("BlockStateSetInfo", clientId, chainId, version));
        await blockStateSetInfoGrain.SetBlockStateSetStatusAsync(new BlockStateSetStatus
        {
            LastIrreversibleBlockHeight = 0,
        });
        
        await blockScanExecutorGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(80);
        
        await blockStateSetInfoGrain.SetBlockStateSetStatusAsync(new BlockStateSetStatus
        {
            LastIrreversibleBlockHeight = 10,
        });
        
        await blockScanExecutorGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(90);
        
        await blockScanGrain.SetNewBlockStartHeightAsync(9);
        
        await blockScanExecutorGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(90);
        
        await blockScanExecutorGrain.HandleConfirmedBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(90);
    }
}