using AElfIndexer.Block.Dtos;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.Grain.Chains;
using AElfIndexer.Orleans.TestBase;
using Orleans;
using Shouldly;
using Xunit;

namespace AElfIndexer.EntityEventHandler.Core.Tests;

public class BlockIndexHandlerTests : EntityEventHandlerCoreBlockIndexTestBase
{
    private IBlockIndexHandler _blockIndexHandler;
    private IClusterClient _clusterClient;

    public BlockIndexHandlerTests()
    {
        _blockIndexHandler = GetRequiredService<IBlockIndexHandler>();
        _clusterClient = GetRequiredService<ClusterFixture>().Cluster.Client;
    }

    [Fact]
    public async Task ProcessNewBlockTest()
    {
        var chainId = "AELF";
        var scanId = "ScanId";
        
        var clientManagerGrain = _clusterClient.GetGrain<IBlockScanManagerGrain>(0);
        await clientManagerGrain.AddBlockScanAsync(chainId, scanId);

        await _blockIndexHandler.ProcessNewBlockAsync(new BlockWithTransactionDto
        {
            ChainId = "AELF",
            BlockHeight = 100,
            BlockHash = "BlockHash"
        });

        var chainGrain = _clusterClient.GetGrain<IChainGrain>("AELF");
        var chainStatus = await chainGrain.GetChainStatusAsync();
        chainStatus.BlockHash.ShouldBe("BlockHash");
        chainStatus.BlockHeight.ShouldBe(100);
    }
    
    [Fact]
    public async Task ProcessConfirmedBlocksTest()
    {
        var chainId = "AELF";
        var scanId = "ScanId";
        
        var clientManagerGrain = _clusterClient.GetGrain<IBlockScanManagerGrain>(0);
        await clientManagerGrain.AddBlockScanAsync(chainId, scanId);
        
        var chainGrain = _clusterClient.GetGrain<IChainGrain>("AELF");
        await chainGrain.SetLatestConfirmBlockAsync("BlockHash100", 100);

        await _blockIndexHandler.ProcessConfirmedBlocksAsync(new List<BlockWithTransactionDto>{new BlockWithTransactionDto
        {
            ChainId = "AELF",
            BlockHeight = 90,
            BlockHash = "BlockHash90"
        }});

        var chainStatus = await chainGrain.GetChainStatusAsync();
        chainStatus.ConfirmedBlockHash.ShouldBe("BlockHash100");
        chainStatus.ConfirmedBlockHeight.ShouldBe(100);
        
        await _blockIndexHandler.ProcessConfirmedBlocksAsync(new List<BlockWithTransactionDto>{new BlockWithTransactionDto
        {
            ChainId = "AELF",
            BlockHeight = 111,
            BlockHash = "BlockHash111",
            PreviousBlockHash = "BlockHash110"
        }});
        
        chainStatus = await chainGrain.GetChainStatusAsync();
        chainStatus.ConfirmedBlockHash.ShouldBe("BlockHash111");
        chainStatus.ConfirmedBlockHeight.ShouldBe(111);
        
        await _blockIndexHandler.ProcessConfirmedBlocksAsync(new List<BlockWithTransactionDto>{new BlockWithTransactionDto
        {
            ChainId = "AELF",
            BlockHeight = 121,
            BlockHash = "BlockHash121",
            PreviousBlockHash = "BlockHash120"
        }});
        
        chainStatus = await chainGrain.GetChainStatusAsync();
        chainStatus.ConfirmedBlockHash.ShouldBe("BlockHash111");
        chainStatus.ConfirmedBlockHeight.ShouldBe(111);
        
        await _blockIndexHandler.ProcessConfirmedBlocksAsync(new List<BlockWithTransactionDto>{new BlockWithTransactionDto
        {
            ChainId = "AELF",
            BlockHeight = 112,
            BlockHash = "BlockHash112",
            PreviousBlockHash = "BlockHash111"
        }});
        
        chainStatus = await chainGrain.GetChainStatusAsync();
        chainStatus.ConfirmedBlockHash.ShouldBe("BlockHash112");
        chainStatus.ConfirmedBlockHeight.ShouldBe(112);
    }
}