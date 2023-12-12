using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.Grain.Chains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.BlockScan;
using Orleans.Streams;
using Shouldly;
using Xunit;

namespace AElfIndexer.Grains.BlockScan;

[Collection(ClusterCollection.Name)]
public class BlockScanGrainTests : AElfIndexerGrainTestBase
{
    private readonly IBlockDataProvider _blockDataProvider;

    public BlockScanGrainTests()
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
        await chainGrain.SetLatestConfirmBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash, _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>{new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = true,
            StartBlockNumber = 21,
            FilterType = BlockFilterType.Block
        }});

        await clientGrain.SetTokenAsync(version);
        await clientGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateGrainId(chainId, clientId, version, BlockFilterType.Block);

        var blockScanInfoGrain = Cluster.Client.GetGrain<IBlockScanInfoGrain>(id);
        await blockScanInfoGrain.InitializeAsync(chainId, clientId, version, new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = false,
            StartBlockNumber = 21
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
            {
                v.ChainId.ShouldBe(chainId);
                v.ClientId.ShouldBe(clientId);
                v.Version.ShouldBe(version);
                v.FilterType.ShouldBe(BlockFilterType.Block);
                subscribedBlock.AddRange(v.Blocks);
           return Task.CompletedTask;
        });

        await scanGrain.HandleBlockAsync(new BlockWithTransactionDto());
        subscribedBlock.Count.ShouldBe(0);

        await scanGrain.HandleHistoricalBlockAsync();
        
        subscribedBlock.Count.ShouldBe(50);
        subscribedBlock.Count(o => o.Confirmed).ShouldBe(25);

        var clientInfo = await blockScanInfoGrain.GetClientInfoAsync();
        clientInfo.ScanModeInfo.ScanMode.ShouldBe(ScanMode.NewBlock);
        clientInfo.ScanModeInfo.ScanNewBlockStartHeight.ShouldBe(46);
        
        await scanGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(50);
        
        await scanGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await scanGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await scanGrain.HandleBlockAsync(_blockDataProvider.Blocks[49].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await scanGrain.HandleBlockAsync(_blockDataProvider.Blocks[51].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);
        
        await scanGrain.HandleBlockAsync(_blockDataProvider.Blocks[53].First());
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
        await chainGrain.SetLatestConfirmBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);

        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = chainId,
                OnlyConfirmedBlock = true,
                StartBlockNumber = 21,
                FilterType = BlockFilterType.Block
            }
        });
        await clientGrain.SetTokenAsync(version);
        
        var id = GrainIdHelper.GenerateGrainId(chainId, clientId, version, BlockFilterType.Block);

        var blockScanInfoGrain = Cluster.Client.GetGrain<IBlockScanInfoGrain>(id);
        await blockScanInfoGrain.InitializeAsync(chainId, clientId, version, new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = false,
            StartBlockNumber = 21
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.ClientId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            v.FilterType.ShouldBe(BlockFilterType.Block);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await scanGrain.HandleBlockAsync(new BlockWithTransactionDto());
        subscribedBlock.Count.ShouldBe(0);

        await clientGrain.StopAsync(version);
        
        await scanGrain.HandleHistoricalBlockAsync();

        subscribedBlock.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Block_MissingBlock_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync("BlockHash1000", 1000);
        await chainGrain.SetLatestConfirmBlockAsync("BlockHash1000", 1000);

        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>
        {
            new SubscriptionInfo
            {
                ChainId = chainId,
                OnlyConfirmedBlock = true,
                StartBlockNumber = 200,
                FilterType = BlockFilterType.Block
            }
        });

        await clientGrain.SetTokenAsync(version);
        await clientGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateGrainId(chainId, clientId, version, BlockFilterType.Block);

        var blockScanInfoGrain = Cluster.Client.GetGrain<IBlockScanInfoGrain>(id);
        await blockScanInfoGrain.InitializeAsync(chainId, clientId, version, new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = false,
            StartBlockNumber = 200
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        await scanGrain.InitializeAsync(chainId, clientId, version);

        await Assert.ThrowsAsync<ApplicationException>(async () => await scanGrain.HandleHistoricalBlockAsync());

        await scanGrain.ReScanAsync(180);
        await blockScanInfoGrain.SetScanNewBlockStartHeightAsync(180);
        
        await Assert.ThrowsAsync<ApplicationException>(async () => await scanGrain.HandleBlockAsync(new BlockWithTransactionDto
        {
            BlockHash = "BlockHash200",
            BlockHeight = 200
        }));
        
        await Assert.ThrowsAsync<ApplicationException>(async () => await scanGrain.HandleConfirmedBlockAsync(new BlockWithTransactionDto
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
        await chainGrain.SetLatestConfirmBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);

        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>{new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = true,
            StartBlockNumber = 21,
            FilterType = BlockFilterType.Block
        }});
        await clientGrain.SetTokenAsync(version);
        await clientGrain.StartAsync(version);

        var id = GrainIdHelper.GenerateGrainId(chainId, clientId, version, BlockFilterType.Block);

        var blockScanInfoGrain = Cluster.Client.GetGrain<IBlockScanInfoGrain>(id);
        await blockScanInfoGrain.InitializeAsync(chainId, clientId, version, new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = true,
            StartBlockNumber = 21,
            FilterType = BlockFilterType.Block
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.ClientId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            v.FilterType.ShouldBe(BlockFilterType.Block);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await scanGrain.HandleConfirmedBlockAsync(new BlockWithTransactionDto());
        subscribedBlock.Count.ShouldBe(0);

        await scanGrain.HandleHistoricalBlockAsync();
        
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
        await chainGrain.SetLatestConfirmBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>{new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = true,
            StartBlockNumber = 21,
            FilterType = BlockFilterType.Block
        }});
        await clientGrain.SetTokenAsync(version);
        await clientGrain.StartAsync(version);

        var id = GrainIdHelper.GenerateGrainId(chainId, clientId, version, BlockFilterType.Block);

        var blockScanInfoGrain = Cluster.Client.GetGrain<IBlockScanInfoGrain>(id);
        await blockScanInfoGrain.InitializeAsync(chainId, clientId, version, new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = false,
            StartBlockNumber = 21
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.ClientId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            v.FilterType.ShouldBe(BlockFilterType.Block);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });
        
        await scanGrain.HandleHistoricalBlockAsync();
        
        subscribedBlock.Count.ShouldBe(50);
        
        await scanGrain.HandleConfirmedBlockAsync(_blockDataProvider.Blocks[46].First());
        subscribedBlock.Count.ShouldBe(50);
    }

    [Theory]
    [InlineData(BlockFilterType.Block)]
    [InlineData(BlockFilterType.Transaction)]
    [InlineData(BlockFilterType.LogEvent)]
    public async Task Block_WithFilter_Test(BlockFilterType filterType)
    {
        var chainId = "AELF";
        var clientId = "DApp";

        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash,
            _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash,
            _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>{new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = false,
            StartBlockNumber = 21,
            FilterType = filterType,
            SubscribeEvents = new List<FilterContractEventInput>
            {
                new FilterContractEventInput
                {
                    ContractAddress = "ContractAddress0",
                    EventNames = new List<string> { "EventName30", "EventName50" }
                }
            }
        }});
        await clientGrain.SetTokenAsync(version);
        await clientGrain.StartAsync(version);

        var id = GrainIdHelper.GenerateGrainId(chainId, clientId, version, BlockFilterType.Block);

        var blockScanInfoGrain = Cluster.Client.GetGrain<IBlockScanInfoGrain>(id);
        await blockScanInfoGrain.InitializeAsync(chainId, clientId, version, new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = false,
            StartBlockNumber = 21,
            FilterType = filterType,
            SubscribeEvents = new List<FilterContractEventInput>
            {
                new FilterContractEventInput
                {
                    ContractAddress = "ContractAddress0",
                    EventNames = new List<string> { "EventName30", "EventName50" }
                }
            }
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockWithTransactionDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.ClientId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            v.FilterType.ShouldBe(filterType);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await scanGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(50);
        subscribedBlock.Last().BlockHeight.ShouldBe(45);

        await scanGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);

        await scanGrain.HandleConfirmedBlockAsync( _blockDataProvider.Blocks[50].First() );
        subscribedBlock.Count.ShouldBe(60);
        subscribedBlock.Last().BlockHeight.ShouldBe(50);

        if (filterType != BlockFilterType.Block)
        {
            foreach (var block in subscribedBlock)
            {
                if (block.BlockHeight == 30 || block.BlockHeight == 50)
                {

                    block.Transactions.Count.ShouldBe(1);
                }
                else
                {
                    block.Transactions.Count.ShouldBe(0);
                }
            }
        }
    }

    [Fact]
    public async Task Block_BlockPushThreshold_Test()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash, _blockDataProvider.Blocks[60].First().BlockHeight);
        await chainGrain.SetLatestConfirmBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash, _blockDataProvider.Blocks[50].First().BlockHeight);
        
        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(clientId);
        var version = await clientGrain.AddSubscriptionInfoAsync(new List<SubscriptionInfo>{new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = true,
            StartBlockNumber = 1,
            FilterType = BlockFilterType.Block
        }});

        await clientGrain.SetTokenAsync(version);
        await clientGrain.StartAsync(version);
        var id = GrainIdHelper.GenerateGrainId(chainId, clientId, version, BlockFilterType.Block);

        var blockScanInfoGrain = Cluster.Client.GetGrain<IBlockScanInfoGrain>(id);
        await blockScanInfoGrain.InitializeAsync(chainId, clientId, version, new SubscriptionInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = false,
            StartBlockNumber = 1
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
            {
                v.ChainId.ShouldBe(chainId);
                v.ClientId.ShouldBe(clientId);
                v.Version.ShouldBe(version);
                v.FilterType.ShouldBe(BlockFilterType.Block);
                subscribedBlock.AddRange(v.Blocks);
           return Task.CompletedTask;
        });
        
        var blockStateSetInfoGrain = Cluster.Client.GetGrain<IBlockStateSetInfoGrain>(
            GrainIdHelper.GenerateGrainId("BlockStateSetInfo", clientId, chainId, version));
        await blockStateSetInfoGrain.SetConfirmedBlockHeight(BlockFilterType.Block, 0);
        
        await scanGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(80);
        
        await blockStateSetInfoGrain.SetConfirmedBlockHeight(BlockFilterType.Block, 10);
        
        await scanGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(90);
        
        await blockScanInfoGrain.SetScanNewBlockStartHeightAsync(9);
        
        await scanGrain.HandleBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(90);
        
        await scanGrain.HandleConfirmedBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(90);
    }
}