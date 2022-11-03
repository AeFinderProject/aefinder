using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScan.AElf.Dtos;
using AElfScan.Grains.Grain.BlockScan;
using AElfScan.Grains.Grain.Chains;
using AElfScan.Grains.State.BlockScan;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using Orleans.Streams;
using Shouldly;
using Xunit;

namespace AElfScan.Grains.BlockScan;

[Collection(ClusterCollection.Name)]
public class BlockScanGrainTests : AElfScanGrainTestBase
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
        var version = Guid.NewGuid().ToString();
        var id = chainId + clientId;
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash, _blockDataProvider.Blocks[60].First().BlockNumber);
        await chainGrain.SetLatestConfirmBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash, _blockDataProvider.Blocks[50].First().BlockNumber);

        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(id);
        await clientGrain.InitializeAsync(chainId, clientId, version, new SubscribeInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = false,
            StartBlockNumber = 21
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfScanApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfScanApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
            {
                v.ChainId.ShouldBe(chainId);
                v.ClientId.ShouldBe(clientId);
                v.Version.ShouldBe(version);
                subscribedBlock.AddRange(v.Blocks);
           return Task.CompletedTask;
        });

        await scanGrain.HandleNewBlockAsync(new BlockDto());
        subscribedBlock.Count.ShouldBe(0);

        await scanGrain.HandleHistoricalBlockAsync();
        
        subscribedBlock.Count.ShouldBe(50);
        subscribedBlock.Count(o => o.IsConfirmed).ShouldBe(25);

        var clientInfo = await clientGrain.GetClientInfoAsync();
        clientInfo.ScanModeInfo.ScanMode.ShouldBe(ScanMode.NewBlock);
        clientInfo.ScanModeInfo.ScanNewBlockStartHeight.ShouldBe(46);
        
        await scanGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(50);
        
        await scanGrain.HandleNewBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockNumber.ShouldBe(50);
        
        await scanGrain.HandleNewBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockNumber.ShouldBe(50);
        
        await scanGrain.HandleNewBlockAsync(_blockDataProvider.Blocks[49].First());
        subscribedBlock.Count.ShouldBe(55);
        subscribedBlock.Last().BlockNumber.ShouldBe(50);
        
        await scanGrain.HandleNewBlockAsync(_blockDataProvider.Blocks[51].First());
        subscribedBlock.Count.ShouldBe(56);
        subscribedBlock.Last().BlockNumber.ShouldBe(51);
        
        await scanGrain.HandleNewBlockAsync(_blockDataProvider.Blocks[53].First());
        subscribedBlock.Count.ShouldBe(61);
        subscribedBlock.Last().BlockNumber.ShouldBe(53);
        
        await scanGrain.HandleNewBlockAsync(_blockDataProvider.Blocks[54].First());
        subscribedBlock.Count.ShouldBe(62);
        subscribedBlock.Last().BlockNumber.ShouldBe(54);
        
        await scanGrain.HandleConfirmedBlockAsync(new List<BlockDto>{_blockDataProvider.Blocks[46].First()});
        subscribedBlock.Count.ShouldBe(63);
        subscribedBlock.Last().BlockNumber.ShouldBe(46);
        
        await scanGrain.HandleConfirmedBlockAsync(new List<BlockDto>{_blockDataProvider.Blocks[48].First()});
        subscribedBlock.Count.ShouldBe(65);
        subscribedBlock.Last().BlockNumber.ShouldBe(48);
        
        await scanGrain.HandleConfirmedBlockAsync(new List<BlockDto>{_blockDataProvider.Blocks[56].First()});
        subscribedBlock.Count.ShouldBe(65);
        subscribedBlock.Last().BlockNumber.ShouldBe(48);
    }
    
    [Fact]
    public async Task OnlyConfirmedBlockTest()
    {
        var chainId = "AELF";
        var clientId = "DApp";
        var version = Guid.NewGuid().ToString();
        var id = chainId + clientId;
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash, _blockDataProvider.Blocks[60].First().BlockNumber);
        await chainGrain.SetLatestConfirmBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash, _blockDataProvider.Blocks[50].First().BlockNumber);

        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(id);
        await clientGrain.InitializeAsync(chainId, clientId, version, new SubscribeInfo
        {
            ChainId = chainId,
            OnlyConfirmedBlock = true,
            StartBlockNumber = 21
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client 
                .GetStreamProvider(AElfScanApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfScanApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
        {
            v.ChainId.ShouldBe(chainId);
            v.ClientId.ShouldBe(clientId);
            v.Version.ShouldBe(version);
            subscribedBlock.AddRange(v.Blocks);
            return Task.CompletedTask;
        });

        await scanGrain.HandleHistoricalBlockAsync();
        
        subscribedBlock.Count.ShouldBe(25);
        var number = 21;
        foreach (var block in subscribedBlock)
        {
            block.BlockNumber.ShouldBe(number);
            block.IsConfirmed.ShouldBe(true);
            number++;
        }
    }
    
    [Theory]
    [InlineData(BlockFilterType.Block)]
    [InlineData(BlockFilterType.Transaction)]
    [InlineData(BlockFilterType.LogEvent)]
    public async Task Block_WithFilter_Test(BlockFilterType filterType)
    {
        var chainId = "AELF";
        var clientId = "DApp";
        var version = Guid.NewGuid().ToString();
        var id = chainId + clientId;
        
        var chainGrain = Cluster.Client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync(_blockDataProvider.Blocks[60].First().BlockHash, _blockDataProvider.Blocks[60].First().BlockNumber);
        await chainGrain.SetLatestConfirmBlockAsync(_blockDataProvider.Blocks[50].First().BlockHash, _blockDataProvider.Blocks[50].First().BlockNumber);

        var clientGrain = Cluster.Client.GetGrain<IClientGrain>(id);
        await clientGrain.InitializeAsync(chainId, clientId, version, new SubscribeInfo
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
                    EventNames = new List<string>{"EventName30","EventName50"}
                }
            }
        });

        var scanGrain = Cluster.Client.GetGrain<IBlockScanGrain>(id);
        var streamId = await scanGrain.InitializeAsync(chainId, clientId, version);
        var stream =
            Cluster.Client
                .GetStreamProvider(AElfScanApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfScanApplicationConsts.MessageStreamNamespace);

        var subscribedBlock = new List<BlockDto>();
        await stream.SubscribeAsync((v, t) =>
            {
                v.ChainId.ShouldBe(chainId);
                v.ClientId.ShouldBe(clientId);
                v.Version.ShouldBe(version);
                subscribedBlock.AddRange(v.Blocks);
           return Task.CompletedTask;
        });
        
        await scanGrain.HandleHistoricalBlockAsync();
        subscribedBlock.Count.ShouldBe(2);
        subscribedBlock.Last().BlockNumber.ShouldBe(30);
        
        await scanGrain.HandleNewBlockAsync(_blockDataProvider.Blocks[50].First());
        subscribedBlock.Count.ShouldBe(3);
        subscribedBlock.Last().BlockNumber.ShouldBe(50);
        
        await scanGrain.HandleConfirmedBlockAsync(new List<BlockDto>{_blockDataProvider.Blocks[50].First()});
        subscribedBlock.Count.ShouldBe(4);
        subscribedBlock.Last().BlockNumber.ShouldBe(50);
    }
}