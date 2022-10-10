using AElf.Indexing.Elasticsearch;
using AElfScan.AElf;
using AElfScan.AElf.Entities.Es;
using AElfScan.AElf.Etos;
using Nest;
using Shouldly;
using Volo.Abp.EventBus.Distributed;
using Xunit;
using Index = System.Index;

namespace AElfScan.EntityEventHandler.Core.Tests.AElf;

public class BlockHandlerTests:AElfScanEntityEventHandlerCoreTestBase
{
    private readonly IDistributedEventHandler<NewBlockEto> _newBlockEventHandler;
    private readonly IDistributedEventHandler<ConfirmBlocksEto> _confirmBlockEventHandler;
    private readonly INESTRepository<Block, Guid> _blockIndexRepository;

    public BlockHandlerTests()
    {
        _newBlockEventHandler = GetRequiredService<BlockHandler>();
        _confirmBlockEventHandler = GetRequiredService<BlockHandler>();
        _blockIndexRepository = GetRequiredService<INESTRepository<Block, Guid>>();
    }

    [Fact]
    public async Task HandleEvent_NewBlock_Test10_11()
    {
        //Unit test 10
        var newBlockEto_h1 = MockDataHelper.MockNewBlockEtoData(1, MockDataHelper.CreateBlockHash());
        var newBlockEto_h2 = MockDataHelper.MockNewBlockEtoData(2, newBlockEto_h1.BlockHash);
        var newBlockEto_h3 = MockDataHelper.MockNewBlockEtoData(3, newBlockEto_h2.BlockHash);
        var newBlockEto_h4 = MockDataHelper.MockNewBlockEtoData(4, newBlockEto_h3.BlockHash);
        var newBlockEto_h5 = MockDataHelper.MockNewBlockEtoData(5, newBlockEto_h4.BlockHash);

        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h1);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h2);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h3);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h4);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h5);

        Thread.Sleep(2000);
        var blockIndex_h1 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h1.BlockHash)));
        blockIndex_h1.ShouldNotBeNull();
        var blockIndex_h2 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h2.BlockHash)));
        blockIndex_h2.ShouldNotBeNull();
        var blockIndex_h3 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h3.BlockHash)));
        blockIndex_h3.ShouldNotBeNull();
        var blockIndex_h4 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h4.BlockHash)));
        blockIndex_h4.ShouldNotBeNull();
        var blockIndex_h5 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h5.BlockHash)));
        blockIndex_h5.ShouldNotBeNull();
        
        //Unit test 11
        var newBlockEto_h6 = MockDataHelper.MockNewBlockEtoData(6, newBlockEto_h5.BlockHash);
        var newBlockEto_h7 = MockDataHelper.MockNewBlockEtoData(7, newBlockEto_h6.BlockHash);
        var newBlockEto_h8 = MockDataHelper.MockNewBlockEtoData(8, newBlockEto_h7.BlockHash);
        var newBlockEto_h9 = MockDataHelper.MockNewBlockEtoData(newBlockEto_h8.BlockHash,9, newBlockEto_h8.BlockHash);
        var newBlockEto_h10 = MockDataHelper.MockNewBlockEtoData(10, newBlockEto_h9.BlockHash);
        
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h6);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h7);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h8);
        Thread.Sleep(1500);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h9);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h10);
        
        Thread.Sleep(1500);
        
        var blockIndex_h8 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h8.BlockHash)) &&
            q.Term(i => i.Field(f => f.BlockNumber).Value(newBlockEto_h8.BlockNumber)));
        blockIndex_h8.ShouldNotBeNull();
        
        var blockIndex_h9 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h9.BlockHash)) &&
            q.Term(i => i.Field(f => f.BlockNumber).Value(newBlockEto_h9.BlockNumber)));
        blockIndex_h9.ShouldBeNull();
    }

    [Fact]
    public async Task HandleEvent_NewBlock_Test12()
    {
        var newBlockEto_h11 = MockDataHelper.MockNewBlockEtoData(11, MockDataHelper.CreateBlockHash());
        var newBlockEto_h13 = MockDataHelper.MockNewBlockEtoData(13, MockDataHelper.CreateBlockHash());
        var newBlockEto_h15 = MockDataHelper.MockNewBlockEtoData(15, MockDataHelper.CreateBlockHash());
        
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h11);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h13);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h15);
        
        Thread.Sleep(1500);
        
        var blockIndex_h11 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h11.BlockHash)));
        blockIndex_h11.ShouldNotBeNull();
        var blockIndex_h13 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h13.BlockHash)));
        blockIndex_h13.ShouldNotBeNull();
        var blockIndex_h15 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h15.BlockHash)));
        blockIndex_h15.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleEvent_ConfirmBlock_Test13_14()
    {
        //Unit test 13
        var newBlockEto_h21 = MockDataHelper.MockNewBlockEtoData(21, MockDataHelper.CreateBlockHash());
        var newBlockEto_h22 = MockDataHelper.MockNewBlockEtoData(22, newBlockEto_h21.BlockHash);
        var newBlockEto_h23 = MockDataHelper.MockNewBlockEtoData(23, newBlockEto_h22.BlockHash);
        var newBlockEto_h24 = MockDataHelper.MockNewBlockEtoData(24, newBlockEto_h23.BlockHash);
        var newBlockEto_h25 = MockDataHelper.MockNewBlockEtoData(25, newBlockEto_h24.BlockHash);
        var newBlockEto_h26 = MockDataHelper.MockNewBlockEtoData(26, newBlockEto_h25.BlockHash);
        var newBlockEto_h27 = MockDataHelper.MockNewBlockEtoData(27, newBlockEto_h26.BlockHash);
        var newBlockEto_h27_fork = MockDataHelper.MockNewBlockEtoData(27, newBlockEto_h26.BlockHash);
        var newBlockEto_h28 = MockDataHelper.MockNewBlockEtoData(28, newBlockEto_h27.BlockHash);
        var newBlockEto_h28_fork = MockDataHelper.MockNewBlockEtoData(28, newBlockEto_h27.BlockHash);
        var newBlockEto_h29 = MockDataHelper.MockNewBlockEtoData(29, newBlockEto_h28.BlockHash);
        var newBlockEto_h29_fork = MockDataHelper.MockNewBlockEtoData(29, newBlockEto_h28.BlockHash);
        var newBlockEto_h30 = MockDataHelper.MockNewBlockEtoData(30, newBlockEto_h29.BlockHash);
        
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h21);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h22);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h23);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h24);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h25);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h26);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h27);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h27_fork);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h28);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h28_fork);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h29);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h29_fork);
        await _newBlockEventHandler.HandleEventAsync(newBlockEto_h30);
        
        Thread.Sleep(2000);

        var confirmBlockEto_h21 = MockDataHelper.MockConfirmBlockEtoData(newBlockEto_h21.BlockHash, 21);
        var confirmBlockEto_h22 = MockDataHelper.MockConfirmBlockEtoData(newBlockEto_h22.BlockHash, 22);
        var confirmBlockEto_h23 = MockDataHelper.MockConfirmBlockEtoData(newBlockEto_h23.BlockHash, 23);
        var confirmBlockEto_h24 = MockDataHelper.MockConfirmBlockEtoData(newBlockEto_h24.BlockHash, 24);
        var confirmBlockEto_h25 = MockDataHelper.MockConfirmBlockEtoData(newBlockEto_h25.BlockHash, 25);
        var confirmBlockEto_h26 = MockDataHelper.MockConfirmBlockEtoData(newBlockEto_h26.BlockHash, 26);
        var confirmBlockEto_h27 = MockDataHelper.MockConfirmBlockEtoData(newBlockEto_h27.BlockHash, 27);
        var confirmBlockEto_h28 = MockDataHelper.MockConfirmBlockEtoData(newBlockEto_h28.BlockHash, 28);

        List<ConfirmBlockEto> confirmBlockEtoList = new List<ConfirmBlockEto>();
        confirmBlockEtoList.Add(confirmBlockEto_h21);
        confirmBlockEtoList.Add(confirmBlockEto_h22);
        confirmBlockEtoList.Add(confirmBlockEto_h23);
        confirmBlockEtoList.Add(confirmBlockEto_h24);
        confirmBlockEtoList.Add(confirmBlockEto_h25);
        confirmBlockEtoList.Add(confirmBlockEto_h26);
        confirmBlockEtoList.Add(confirmBlockEto_h27);
        confirmBlockEtoList.Add(confirmBlockEto_h28);
        ConfirmBlocksEto confirmBlocksEto = new ConfirmBlocksEto()
        {
            ConfirmBlocks = confirmBlockEtoList
        };
        await _confirmBlockEventHandler.HandleEventAsync(confirmBlocksEto);
        
        Thread.Sleep(2000);
        
        var blockIndex_h27_fork = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h27_fork.BlockHash)));
        blockIndex_h27_fork.ShouldBeNull();
        var blockIndex_h28_fork = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h28_fork.BlockHash)));
        blockIndex_h28_fork.ShouldBeNull();
        var blockIndex_h29_fork = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h29_fork.BlockHash)));
        blockIndex_h29_fork.ShouldNotBeNull();
        
        var mustQuery = new List<Func<QueryContainerDescriptor<Block>, QueryContainer>>();
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).GreaterThanOrEquals(21)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockNumber).LessThanOrEquals(28)));
        QueryContainer Filter(QueryContainerDescriptor<Block> f) => f.Bool(b => b.Must(mustQuery));
        var blockIndex_h21_28=await _blockIndexRepository.GetListAsync(Filter);
        foreach (var blockItem in blockIndex_h21_28.Item2)
        {
            blockItem.IsConfirmed.ShouldBeTrue();
        }
        var blockIndex_h29 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h29.BlockHash)));
        blockIndex_h29.IsConfirmed.ShouldBeFalse();
        blockIndex_h29_fork.IsConfirmed.ShouldBeFalse();
        var blockIndex_h30 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h30.BlockHash)));
        blockIndex_h30.IsConfirmed.ShouldBeFalse();
        
        
        //Unit test 14
        var confirmBlockEto_h31 = MockDataHelper.MockConfirmBlockEtoData(MockDataHelper.CreateBlockHash(), 31);
        ConfirmBlocksEto confirmBlocks = new ConfirmBlocksEto()
        {
            ConfirmBlocks = new List<ConfirmBlockEto>(){confirmBlockEto_h31}
        };
        
        try
        {
            await _confirmBlockEventHandler.HandleEventAsync(confirmBlocks);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        
        Thread.Sleep(1000);
        blockIndex_h30 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h30.BlockHash)));
        blockIndex_h30.IsConfirmed.ShouldBeFalse();
        
        var blockIndex_h31 = await _blockIndexRepository.GetAsync(q =>
            q.Term(i => i.Field(f => f.BlockHash).Value(confirmBlockEto_h31.BlockHash)));
        blockIndex_h31.ShouldBeNull();
    }
    
}