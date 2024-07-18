using AeFinder.BlockChainEventHandler.DTOs;
using AeFinder.BlockChainEventHandler.Processors;
using AeFinder.BlockChainEventHandler.Providers;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Blocks;
using AeFinder.Orleans.TestBase;
using Shouldly;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace AeFinder.BlockChainEventHandler;

[Collection(ClusterCollection.Name)]
public sealed class BlockChainDataEventHandlerTests:AeFinderBlockChainEventHandlerCoreTestBase
{
    private readonly IDistributedEventHandler<BlockChainDataEto> _blockChainDataEventHandler;
    private readonly IBlockGrainProvider _blockGrainProvider;

    public BlockChainDataEventHandlerTests()
    {
        _blockChainDataEventHandler = GetRequiredService<BlockChainDataEventHandler>();
        _blockGrainProvider = GetRequiredService<IBlockGrainProvider>();
    }

    // private readonly int grainPrimaryKey = 57;

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
        
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h63);
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
        var blockChainDataEto_h66_fork = MockDataHelper.MockEtoDataWithTransactions(66, blockChainDataEto_h65.Blocks[0].BlockHash);
        var blockChainDataEto_h67_fork = MockDataHelper.MockEtoDataWithTransactions(67, blockChainDataEto_h66.Blocks[0].BlockHash);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h66_fork);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h67_fork);
        
        //Unit test 4
        var blockChainDataEto_h83 = MockDataHelper.MockEtoDataWithTransactions(83, blockChainDataEto_h82.Blocks[0].BlockHash);
        var blockChainDataEto_h84 = MockDataHelper.MockEtoDataWithTransactions(84, blockChainDataEto_h83.Blocks[0].BlockHash);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h83);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h84);
        
        var grain = await _blockGrainProvider.GetBlockBranchGrain("AELF");
        var blockDictionary = await grain.GetBlockDictionary();
        
        blockDictionary.ShouldContainKey(blockChainDataEto_h81.Blocks[0].BlockHash);//Unit test 1
        blockDictionary.ShouldContainKey(blockChainDataEto_h82.Blocks[0].BlockHash);//Unit test 2
        blockDictionary.ShouldContainKey(blockChainDataEto_h66_fork.Blocks[0].BlockHash);//Unit test 3
        blockDictionary.ShouldContainKey(blockChainDataEto_h67_fork.Blocks[0].BlockHash);//Unit test 3
        blockDictionary.ShouldContainKey(blockChainDataEto_h84.Blocks[0].BlockHash);//Unit test 4
        
        blockDictionary.Values.ShouldContain(x => x.BlockHeight == 81);
        blockDictionary.Values.ShouldContain(x => x.BlockHeight == 82);
        blockDictionary.Values.ShouldContain(x => x.BlockHeight == 84);
        foreach (var blockItem in blockDictionary)
        {
            if (blockItem.Value.BlockHeight == 81)
            {
                blockItem.Value.ChainId.ShouldBe("AELF");
                blockItem.Key.ShouldBe(blockItem.Value.BlockHash);
                blockItem.Value.PreviousBlockHash.ShouldBe(blockChainDataEto_h80.Blocks[0].BlockHash);
                blockItem.Value.Confirmed.ShouldBeFalse();
            }
            if (blockItem.Value.BlockHeight == 82)
            {
                blockItem.Value.ChainId.ShouldBe("AELF");
                blockItem.Key.ShouldBe(blockItem.Value.BlockHash);
                blockItem.Value.PreviousBlockHash.ShouldBe(blockChainDataEto_h81.Blocks[0].BlockHash);
                blockItem.Value.Confirmed.ShouldBeFalse();
            }
            if (blockItem.Value.BlockHeight == 84)
            {
                blockItem.Value.ChainId.ShouldBe("AELF");
                blockItem.Key.ShouldBe(blockItem.Value.BlockHash);
                blockItem.Value.PreviousBlockHash.ShouldBe(blockChainDataEto_h83.Blocks[0].BlockHash);
                blockItem.Value.Confirmed.ShouldBeFalse();
            }
        }
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
        
        // await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h1);
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
        
        
        var grain = await _blockGrainProvider.GetBlockBranchGrain("AELF");
        var blockDictionary = await grain.GetBlockDictionary();
        
        foreach (var blockItem in blockDictionary)
        {
            blockItem.Value.BlockHeight.ShouldBeGreaterThanOrEqualTo(90);
        
            if (blockItem.Value.BlockHeight == 90)
            {
                blockItem.Value.Confirmed.ShouldBeTrue();
            }
            else
            {
                blockItem.Value.Confirmed.ShouldBeFalse();
            }
        }
        
        blockDictionary.Count(item=>item.Value.BlockHeight==90).ShouldBe(1);
    }

    [Fact]
    public async Task HandleEvent_StorageLogic_Test6()
    {
        var blockChainDataEto_h90 = MockDataHelper.MockBasicEtoData(90, MockDataHelper.CreateBlockHash());
        var blockEto_h95 = MockDataHelper.MockBlockEto(95, blockChainDataEto_h90.Blocks[0].BlockHash);
        var blockEto_h99 = MockDataHelper.MockBlockEto(99, blockEto_h95.BlockHash);
        var blockEto_h100 = MockDataHelper.MockBlockEtoWithLibFoundEvent(100, blockEto_h99.BlockHash, 95);
        var blockEto_h105 = MockDataHelper.MockBlockEto(105, blockEto_h100.BlockHash);
        var blockEto_h110 = MockDataHelper.MockBlockEtoWithLibFoundEvent(110, blockEto_h105.BlockHash, 1);
        blockChainDataEto_h90.Blocks.Add(blockEto_h95);
        blockChainDataEto_h90.Blocks.Add(blockEto_h99);
        blockChainDataEto_h90.Blocks.Add(blockEto_h100);
        blockChainDataEto_h90.Blocks.Add(blockEto_h105);
        blockChainDataEto_h90.Blocks.Add(blockEto_h110);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h90);

        var blockChainDataEto_h111 = MockDataHelper.MockBasicEtoData(111, blockEto_h110.BlockHash);
        var blockEto_h112 = MockDataHelper.MockBlockEto(112, blockChainDataEto_h111.Blocks[0].BlockHash);
        var blockEto_h113 = MockDataHelper.MockBlockEto(113, blockEto_h112.BlockHash);
        var blockEto_h114 = MockDataHelper.MockBlockEto(114, blockEto_h113.BlockHash);
        var blockEto_h115 = MockDataHelper.MockBlockEtoWithLibFoundEvent(115, blockEto_h114.BlockHash, 95);
        var blockEto_h116 = MockDataHelper.MockBlockEto(116, blockEto_h115.BlockHash);
        var blockEto_h117 = MockDataHelper.MockBlockEto(117, blockEto_h116.BlockHash);
        var blockEto_h118 = MockDataHelper.MockBlockEto(118, blockEto_h117.BlockHash);
        var blockEto_h119 = MockDataHelper.MockBlockEto(119, blockEto_h118.BlockHash);
        var blockEto_h120 = MockDataHelper.MockBlockEto(120, blockEto_h119.BlockHash);
        blockChainDataEto_h111.Blocks.Add(blockEto_h112);
        blockChainDataEto_h111.Blocks.Add(blockEto_h113);
        blockChainDataEto_h111.Blocks.Add(blockEto_h114);
        blockChainDataEto_h111.Blocks.Add(blockEto_h115);
        blockChainDataEto_h111.Blocks.Add(blockEto_h116);
        blockChainDataEto_h111.Blocks.Add(blockEto_h117);
        blockChainDataEto_h111.Blocks.Add(blockEto_h118);
        blockChainDataEto_h111.Blocks.Add(blockEto_h119);
        blockChainDataEto_h111.Blocks.Add(blockEto_h120);
        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h111);
        
        var grain = await _blockGrainProvider.GetBlockBranchGrain("AELF");
        var blockDictionary = await grain.GetBlockDictionary();
        
        foreach (var blockItem in blockDictionary)
        {
            blockItem.Value.BlockHeight.ShouldBeGreaterThanOrEqualTo(95);
            
            if (blockItem.Value.BlockHeight == 95)
            {
                blockItem.Value.Confirmed.ShouldBeTrue();
            }
            else
            {
                blockItem.Value.Confirmed.ShouldBeFalse();
            }
        }
    }

    [Fact]
    public async Task HandleEvent_StorageLogic_Test7()
    {
        var blockChainDataEto_h90 = MockDataHelper.MockBasicEtoData(90, MockDataHelper.CreateBlockHash());
        var blockEto_h95 = MockDataHelper.MockBlockEto(95, blockChainDataEto_h90.Blocks[0].BlockHash);
        var blockEto_h99 = MockDataHelper.MockBlockEto(99, blockEto_h95.BlockHash);
        var blockEto_h100 = MockDataHelper.MockBlockEtoWithLibFoundEvent(100, blockEto_h99.BlockHash, 95);
        var blockEto_h105 = MockDataHelper.MockBlockEto(105, blockEto_h100.BlockHash);
        var blockEto_h110 = MockDataHelper.MockBlockEtoWithLibFoundEvent(110, blockEto_h105.BlockHash, 1);
        blockChainDataEto_h90.Blocks.Add(blockEto_h95);
        blockChainDataEto_h90.Blocks.Add(blockEto_h99);
        blockChainDataEto_h90.Blocks.Add(blockEto_h100);
        blockChainDataEto_h90.Blocks.Add(blockEto_h105);
        blockChainDataEto_h90.Blocks.Add(blockEto_h110);

        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h90);

        var blockChainDataEto_h111 = MockDataHelper.MockBasicEtoData(111, blockEto_h110.BlockHash);
        var blockEto_h112 = MockDataHelper.MockBlockEto(112, blockChainDataEto_h111.Blocks[0].BlockHash);
        var blockEto_h113 = MockDataHelper.MockBlockEto(113, blockEto_h112.BlockHash);
        var blockEto_h114 = MockDataHelper.MockBlockEto(113, blockEto_h113.BlockHash);
        var blockEto_h115 = MockDataHelper.MockBlockEtoWithLibFoundEvent(115, blockEto_h114.BlockHash, 95);
        var blockEto_h116 = MockDataHelper.MockBlockEto(116, blockEto_h115.BlockHash);
        var blockEto_h117 = MockDataHelper.MockBlockEto(117, blockEto_h116.BlockHash);
        var blockEto_h118 = MockDataHelper.MockBlockEto(118, blockEto_h117.BlockHash);
        var blockEto_h119 = MockDataHelper.MockBlockEto(119, blockEto_h118.BlockHash);
        var blockEto_h120 = MockDataHelper.MockBlockEto(120, blockEto_h119.BlockHash);
        blockChainDataEto_h111.Blocks.Add(blockEto_h112);
        blockChainDataEto_h111.Blocks.Add(blockEto_h113);
        blockChainDataEto_h111.Blocks.Add(blockEto_h114);
        blockChainDataEto_h111.Blocks.Add(blockEto_h115);
        blockChainDataEto_h111.Blocks.Add(blockEto_h116);
        blockChainDataEto_h111.Blocks.Add(blockEto_h117);
        blockChainDataEto_h111.Blocks.Add(blockEto_h118);
        blockChainDataEto_h111.Blocks.Add(blockEto_h119);
        blockChainDataEto_h111.Blocks.Add(blockEto_h120);

        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h111);

        var blockChainDataEto_h121 = MockDataHelper.MockBasicEtoData(121, blockEto_h120.BlockHash);
        var blockEto_h122 = MockDataHelper.MockBlockEto(122, blockChainDataEto_h121.Blocks[0].BlockHash);
        var blockEto_h123 = MockDataHelper.MockBlockEto(123, blockEto_h122.BlockHash);
        var blockEto_h124 = MockDataHelper.MockBlockEto(124, blockEto_h123.BlockHash);
        var blockEto_h125 = MockDataHelper.MockBlockEtoWithLibFoundEvent(125, blockEto_h124.BlockHash, 100);
        var blockEto_h126 = MockDataHelper.MockBlockEto(126, blockEto_h125.BlockHash);
        var blockEto_h127 = MockDataHelper.MockBlockEto(127, blockEto_h126.BlockHash);
        var blockEto_h128 = MockDataHelper.MockBlockEto(128, blockEto_h127.BlockHash);
        var blockEto_h129 = MockDataHelper.MockBlockEtoWithLibFoundEvent(129, blockEto_h128.BlockHash, 105);
        var blockEto_h130 = MockDataHelper.MockBlockEto(130, blockEto_h129.BlockHash);
        blockChainDataEto_h121.Blocks.Add(blockEto_h122);
        blockChainDataEto_h121.Blocks.Add(blockEto_h123);
        blockChainDataEto_h121.Blocks.Add(blockEto_h124);
        blockChainDataEto_h121.Blocks.Add(blockEto_h125);
        blockChainDataEto_h121.Blocks.Add(blockEto_h126);
        blockChainDataEto_h121.Blocks.Add(blockEto_h127);
        blockChainDataEto_h121.Blocks.Add(blockEto_h128);
        blockChainDataEto_h121.Blocks.Add(blockEto_h129);
        blockChainDataEto_h121.Blocks.Add(blockEto_h130);

        await _blockChainDataEventHandler.HandleEventAsync(blockChainDataEto_h121);
        
        var grain = await _blockGrainProvider.GetBlockBranchGrain("AELF");
        var blockDictionary = await grain.GetBlockDictionary();
        
        foreach (var blockItem in blockDictionary)
        {
            blockItem.Value.BlockHeight.ShouldBeGreaterThanOrEqualTo(105);
        }
    }
}