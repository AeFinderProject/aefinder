using AElfScan.AElf.DTOs;
using AElfScan.AElf.Processors;
using AElfScan.Grain;
using AElfScan.TestInfrastructure;
using Orleans.TestingHost;
using Shouldly;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace AElfScan.BlockChainEventHandler.Core.Tests.AElf;

[Collection(ClusterCollection.Name)]
public sealed class BlockChainDataEventHandlerTests:AElfScanBlockChainEventHandlerCoreTestBase
{
    private readonly IDistributedEventHandler<BlockChainDataEto> _blockChainDataEventHandler;
    private readonly TestCluster _cluster;

    public BlockChainDataEventHandlerTests(ClusterFixture fixture)
    {
        _blockChainDataEventHandler = GetRequiredService<BlockChainDataEventHandler>();
        _cluster = fixture.Cluster;
    }

    

    [Fact]
    public async Task HandleEvent_StorageLogic_Test1_2_3_4()
    {
        var blockChainDataEto_h63 = MockDataHelper.MockBasicEtoData(63,MockDataHelper.CreateBlockHash());
        var blockChainDataEto_h64 = MockDataHelper.MockBasicEtoData(64, blockChainDataEto_h63.Blocks[0].BlockHash);
        var blockChainDataEto_h65 = MockDataHelper.MockBasicEtoData(65, blockChainDataEto_h64.Blocks[0].BlockHash);
        var blockChainDataEto_h66 = MockDataHelper.MockBasicEtoData(66, blockChainDataEto_h65.Blocks[0].BlockHash);
        var blockChainDataEto_h70 = MockDataHelper.MockBasicEtoData(70, blockChainDataEto_h66.Blocks[0].BlockHash);
        var blockChainDataEto_h75 = MockDataHelper.MockBasicEtoData(75, blockChainDataEto_h70.Blocks[0].BlockHash);
        var blockChainDataEto_h80 = MockDataHelper.MockEtoDataWithLibFoundEvent(80, blockChainDataEto_h75.Blocks[0].BlockHash,65);
        
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h64);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h65);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h66);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h70);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h75);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h80);
        //Unit test 1
        var blockChainDataEto_h81 = MockDataHelper.MockBasicEtoData(81, blockChainDataEto_h80.Blocks[0].BlockHash);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h81);
        
        //Unit test 2
        var blockChainDataEto_h82 = MockDataHelper.MockEtoDataWithTransactions(82, blockChainDataEto_h81.Blocks[0].BlockHash);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h82);
        
        //Unit test 3
        var blockChainDataEto_h64_fork = MockDataHelper.MockEtoDataWithTransactions(64, blockChainDataEto_h63.Blocks[0].BlockHash);
        var blockChainDataEto_h65_fork = MockDataHelper.MockEtoDataWithTransactions(65, blockChainDataEto_h64.Blocks[0].BlockHash);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h64_fork);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h65_fork);
        
        //Unit test 4
        var blockChainDataEto_h83 = MockDataHelper.MockEtoDataWithTransactions(83, blockChainDataEto_h82.Blocks[0].BlockHash);
        var blockChainDataEto_h84 = MockDataHelper.MockEtoDataWithTransactions(84, blockChainDataEto_h83.Blocks[0].BlockHash);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h83);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h84);

        var grain = _cluster.GrainFactory.GetGrain<IBlockGrain>(49);
        var blockDictionary = await grain.GetBlockDictionary();
        
        blockDictionary.ShouldContainKey(blockChainDataEto_h81.Blocks[0].BlockHash);//Unit test 1
        blockDictionary.ShouldContainKey(blockChainDataEto_h82.Blocks[0].BlockHash);//Unit test 2
        blockDictionary.ShouldNotContainKey(blockChainDataEto_h64_fork.Blocks[0].BlockHash);//Unit test 3
        blockDictionary.ShouldNotContainKey(blockChainDataEto_h65_fork.Blocks[0].BlockHash);//Unit test 3
        blockDictionary.ShouldContainKey(blockChainDataEto_h84.Blocks[0].BlockHash);//Unit test 4
    }

    [Fact]
    public async Task HandleEvent_StorageLogic_Test5_8_9()
    {
        var blockChainDataEto_h63 = MockDataHelper.MockBasicEtoData(63,MockDataHelper.CreateBlockHash());
        var blockChainDataEto_h64 = MockDataHelper.MockBasicEtoData(64, blockChainDataEto_h63.Blocks[0].BlockHash);
        var blockChainDataEto_h65 = MockDataHelper.MockBasicEtoData(65, blockChainDataEto_h64.Blocks[0].BlockHash);
        var blockChainDataEto_h66 = MockDataHelper.MockBasicEtoData(66, blockChainDataEto_h65.Blocks[0].BlockHash);
        var blockChainDataEto_h70 = MockDataHelper.MockBasicEtoData(70, blockChainDataEto_h66.Blocks[0].BlockHash);
        var blockChainDataEto_h75 = MockDataHelper.MockBasicEtoData(75, blockChainDataEto_h70.Blocks[0].BlockHash);
        var blockChainDataEto_h80 = MockDataHelper.MockEtoDataWithLibFoundEvent(80, blockChainDataEto_h75.Blocks[0].BlockHash,65);
        
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h64);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h65);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h66);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h70);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h75);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h80);
        
        //Unit test 5
        var blockChainDataEto_h90 = MockDataHelper.MockBasicEtoData(90, blockChainDataEto_h80.Blocks[0].BlockHash);
        var blockChainDataEto_h90_fork1 = MockDataHelper.MockBasicEtoData(90, blockChainDataEto_h80.Blocks[0].BlockHash);
        var blockChainDataEto_h90_fork2 = MockDataHelper.MockBasicEtoData(90, blockChainDataEto_h80.Blocks[0].BlockHash);
        var blockChainDataEto_h90_fork3 = MockDataHelper.MockBasicEtoData(90, blockChainDataEto_h80.Blocks[0].BlockHash);
        var blockChainDataEto_h95 = MockDataHelper.MockBasicEtoData(95, blockChainDataEto_h90.Blocks[0].BlockHash);
        var blockChainDataEto_h99 = MockDataHelper.MockBasicEtoData(99, blockChainDataEto_h95.Blocks[0].BlockHash);
        var blockChainDataEto_h100 = MockDataHelper.MockEtoDataWithLibFoundEvent(100, blockChainDataEto_h99.Blocks[0].BlockHash,80);
        var blockChainDataEto_h105 = MockDataHelper.MockBasicEtoData(105, blockChainDataEto_h100.Blocks[0].BlockHash);
        var blockChainDataEto_h110 = MockDataHelper.MockEtoDataWithLibFoundEvent(110, blockChainDataEto_h105.Blocks[0].BlockHash,90);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h90);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h90_fork1);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h90_fork2);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h90_fork3);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h95);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h99);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h100);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h105);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h110);
        
        
        var grain = _cluster.GrainFactory.GetGrain<IBlockGrain>(49);
        var blockDictionary = await grain.GetBlockDictionary();

        foreach (var blockItem in blockDictionary)
        {
            blockItem.Value.BlockNumber.ShouldBeGreaterThanOrEqualTo(90);
        }
        
        blockDictionary.Count(item=>item.Value.BlockNumber==90).ShouldBe(1);
    }

    [Fact]
    public async Task HandleEvent_StorageLogic_Test6()
    {
        var blockChainDataEto_h90 = MockDataHelper.MockBasicEtoData(90, MockDataHelper.CreateBlockHash());
        var blockChainDataEto_h95 = MockDataHelper.MockBasicEtoData(95, blockChainDataEto_h90.Blocks[0].BlockHash);
        var blockChainDataEto_h99 = MockDataHelper.MockBasicEtoData(99, blockChainDataEto_h95.Blocks[0].BlockHash);
        var blockChainDataEto_h100 = MockDataHelper.MockEtoDataWithLibFoundEvent(100, blockChainDataEto_h99.Blocks[0].BlockHash,80);
        var blockChainDataEto_h105 = MockDataHelper.MockBasicEtoData(105, blockChainDataEto_h100.Blocks[0].BlockHash);
        var blockChainDataEto_h110 = MockDataHelper.MockEtoDataWithLibFoundEvent(110, blockChainDataEto_h105.Blocks[0].BlockHash,90);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h90);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h95);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h99);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h100);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h105);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h110);
        
        var blockChainDataEto_h111 = MockDataHelper.MockBasicEtoData(111, blockChainDataEto_h110.Blocks[0].BlockHash);
        var blockChainDataEto_h112 = MockDataHelper.MockBasicEtoData(112, blockChainDataEto_h111.Blocks[0].BlockHash);
        var blockChainDataEto_h113 = MockDataHelper.MockBasicEtoData(113, blockChainDataEto_h112.Blocks[0].BlockHash);
        var blockChainDataEto_h114 = MockDataHelper.MockBasicEtoData(114, blockChainDataEto_h113.Blocks[0].BlockHash);
        var blockChainDataEto_h115 = MockDataHelper.MockEtoDataWithLibFoundEvent(115, blockChainDataEto_h114.Blocks[0].BlockHash,95);
        var blockChainDataEto_h116 = MockDataHelper.MockBasicEtoData(116, blockChainDataEto_h115.Blocks[0].BlockHash);
        var blockChainDataEto_h117 = MockDataHelper.MockBasicEtoData(117, blockChainDataEto_h116.Blocks[0].BlockHash);
        var blockChainDataEto_h118 = MockDataHelper.MockBasicEtoData(118, blockChainDataEto_h117.Blocks[0].BlockHash);
        var blockChainDataEto_h119 = MockDataHelper.MockBasicEtoData(119, blockChainDataEto_h118.Blocks[0].BlockHash);
        var blockChainDataEto_h120 = MockDataHelper.MockBasicEtoData(120, blockChainDataEto_h119.Blocks[0].BlockHash);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h111);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h112);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h113);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h114);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h115);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h116);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h117);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h118);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h119);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h120);
        
        var grain = _cluster.GrainFactory.GetGrain<IBlockGrain>(49);
        var blockDictionary = await grain.GetBlockDictionary();

        foreach (var blockItem in blockDictionary)
        {
            blockItem.Value.BlockNumber.ShouldBeGreaterThanOrEqualTo(95);
        }
    }

    [Fact]
    public async Task HandleEvent_StorageLogic_Test7()
    {
        var blockChainDataEto_h90 = MockDataHelper.MockBasicEtoData(90, MockDataHelper.CreateBlockHash());
        var blockChainDataEto_h95 = MockDataHelper.MockBasicEtoData(95, blockChainDataEto_h90.Blocks[0].BlockHash);
        var blockChainDataEto_h99 = MockDataHelper.MockBasicEtoData(99, blockChainDataEto_h95.Blocks[0].BlockHash);
        var blockChainDataEto_h100 = MockDataHelper.MockEtoDataWithLibFoundEvent(100, blockChainDataEto_h99.Blocks[0].BlockHash,80);
        var blockChainDataEto_h105 = MockDataHelper.MockBasicEtoData(105, blockChainDataEto_h100.Blocks[0].BlockHash);
        var blockChainDataEto_h110 = MockDataHelper.MockEtoDataWithLibFoundEvent(110, blockChainDataEto_h105.Blocks[0].BlockHash,90);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h90);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h95);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h99);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h100);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h105);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h110);
        
        var blockChainDataEto_h111 = MockDataHelper.MockBasicEtoData(111, blockChainDataEto_h110.Blocks[0].BlockHash);
        var blockChainDataEto_h112 = MockDataHelper.MockBasicEtoData(112, blockChainDataEto_h111.Blocks[0].BlockHash);
        var blockChainDataEto_h113 = MockDataHelper.MockBasicEtoData(113, blockChainDataEto_h112.Blocks[0].BlockHash);
        var blockChainDataEto_h114 = MockDataHelper.MockBasicEtoData(114, blockChainDataEto_h113.Blocks[0].BlockHash);
        var blockChainDataEto_h115 = MockDataHelper.MockEtoDataWithLibFoundEvent(115, blockChainDataEto_h114.Blocks[0].BlockHash,95);
        var blockChainDataEto_h116 = MockDataHelper.MockBasicEtoData(116, blockChainDataEto_h115.Blocks[0].BlockHash);
        var blockChainDataEto_h117 = MockDataHelper.MockBasicEtoData(117, blockChainDataEto_h116.Blocks[0].BlockHash);
        var blockChainDataEto_h118 = MockDataHelper.MockBasicEtoData(118, blockChainDataEto_h117.Blocks[0].BlockHash);
        var blockChainDataEto_h119 = MockDataHelper.MockBasicEtoData(119, blockChainDataEto_h118.Blocks[0].BlockHash);
        var blockChainDataEto_h120 = MockDataHelper.MockBasicEtoData(120, blockChainDataEto_h119.Blocks[0].BlockHash);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h111);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h112);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h113);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h114);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h115);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h116);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h117);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h118);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h119);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h120);
        
        var blockChainDataEto_h121 = MockDataHelper.MockBasicEtoData(121, blockChainDataEto_h120.Blocks[0].BlockHash);
        var blockChainDataEto_h122 = MockDataHelper.MockBasicEtoData(122, blockChainDataEto_h121.Blocks[0].BlockHash);
        var blockChainDataEto_h123 = MockDataHelper.MockBasicEtoData(123, blockChainDataEto_h122.Blocks[0].BlockHash);
        var blockChainDataEto_h124 = MockDataHelper.MockBasicEtoData(124, blockChainDataEto_h123.Blocks[0].BlockHash);
        var blockChainDataEto_h125 = MockDataHelper.MockEtoDataWithLibFoundEvent(125, blockChainDataEto_h124.Blocks[0].BlockHash,100);
        var blockChainDataEto_h126 = MockDataHelper.MockBasicEtoData(126, blockChainDataEto_h125.Blocks[0].BlockHash);
        var blockChainDataEto_h127 = MockDataHelper.MockBasicEtoData(127, blockChainDataEto_h126.Blocks[0].BlockHash);
        var blockChainDataEto_h128 = MockDataHelper.MockBasicEtoData(128, blockChainDataEto_h127.Blocks[0].BlockHash);
        var blockChainDataEto_h129 = MockDataHelper.MockEtoDataWithLibFoundEvent(129, blockChainDataEto_h128.Blocks[0].BlockHash,105);
        var blockChainDataEto_h130 = MockDataHelper.MockBasicEtoData(130, blockChainDataEto_h129.Blocks[0].BlockHash);
        
        var grain = _cluster.GrainFactory.GetGrain<IBlockGrain>(49);
        var blockDictionary = await grain.GetBlockDictionary();

        foreach (var blockItem in blockDictionary)
        {
            blockItem.Value.BlockNumber.ShouldBeGreaterThanOrEqualTo(105);
        }
    }
    
}