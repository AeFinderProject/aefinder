using System.Linq.Expressions;
using AeFinder.Entities.Es;
using AeFinder.Etos;
using AElf.EntityMapping.Repositories;
using Shouldly;
using Volo.Abp.EventBus.Distributed;
using Xunit;

namespace AeFinder.EntityEventHandler;

public class BlockHandlerTests:AeFinderEntityEventHandlerCoreTestBase
{
    private readonly IDistributedEventHandler<NewBlocksEto> _newBlockEventHandler;
    private readonly IDistributedEventHandler<ConfirmBlocksEto> _confirmBlockEventHandler;
    private readonly IEntityMappingRepository<BlockIndex, string> _blockIndexRepository;
    private readonly IMockDataHelper _mockDataHelper;

    public BlockHandlerTests()
    {
        _newBlockEventHandler = GetRequiredService<BlockHandler>();
        _confirmBlockEventHandler = GetRequiredService<BlockHandler>();
        _blockIndexRepository = GetRequiredService<IEntityMappingRepository<BlockIndex, string>>();
        _mockDataHelper = GetRequiredService<MockDataHelper>();
    }

    [Fact]
    public async Task HandleEvent_NewBlock_Test10_11()
    {
        //Unit test 10
        var newBlockEto_h1 = _mockDataHelper.MockNewBlockEtoData(1, _mockDataHelper.CreateBlockHash());
        var newBlockEto_h2 = _mockDataHelper.MockNewBlockEtoData(2, newBlockEto_h1.BlockHash);
        var newBlockEto_h3 = _mockDataHelper.MockNewBlockEtoData(3, newBlockEto_h2.BlockHash);
        var newBlockEto_h4 = _mockDataHelper.MockNewBlockEtoData(4, newBlockEto_h3.BlockHash);
        var newBlockEto_h5 = _mockDataHelper.MockNewBlockEtoData(5, newBlockEto_h4.BlockHash);
        
        List<NewBlockEto> newBlockEtoList_1 = new List<NewBlockEto>();
        newBlockEtoList_1.Add(newBlockEto_h1);
        newBlockEtoList_1.Add(newBlockEto_h2);
        newBlockEtoList_1.Add(newBlockEto_h3);
        newBlockEtoList_1.Add(newBlockEto_h4);
        newBlockEtoList_1.Add(newBlockEto_h5);
    
        await _newBlockEventHandler.HandleEventAsync(new NewBlocksEto() { NewBlocks = newBlockEtoList_1 });
    
        var queryable = await _blockIndexRepository.GetQueryableAsync();
        
        // var blockIndex_h1 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h1.BlockHash)));
        var blockIndex_h1 = queryable.Where(p => p.BlockHash == newBlockEto_h1.BlockHash).ToList()[0];
        blockIndex_h1.ShouldNotBeNull();
        // var blockIndex_h2 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h2.BlockHash)));
        var blockIndex_h2 = queryable.Where(p => p.BlockHash == newBlockEto_h2.BlockHash).ToList()[0];
        blockIndex_h2.ShouldNotBeNull();
        // var blockIndex_h3 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h3.BlockHash)));
        var blockIndex_h3 = queryable.Where(p => p.BlockHash == newBlockEto_h3.BlockHash).ToList()[0];
        blockIndex_h3.ShouldNotBeNull();
        // var blockIndex_h4 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h4.BlockHash)));
        var blockIndex_h4 = queryable.Where(p => p.BlockHash == newBlockEto_h4.BlockHash).ToList()[0];
        blockIndex_h4.ShouldNotBeNull();
        // var blockIndex_h5 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h5.BlockHash)));
        var blockIndex_h5 = queryable.Where(p => p.BlockHash == newBlockEto_h5.BlockHash).ToList()[0];
        blockIndex_h5.ShouldNotBeNull();
        
        //Unit test 11
        var newBlockEto_h6 = _mockDataHelper.MockNewBlockEtoData(6, newBlockEto_h5.BlockHash);
        var newBlockEto_h7 = _mockDataHelper.MockNewBlockEtoData(7, newBlockEto_h6.BlockHash);
        var newBlockEto_h8 = _mockDataHelper.MockNewBlockEtoData(8, newBlockEto_h7.BlockHash);
        var newBlockEto_h9 = _mockDataHelper.MockNewBlockEtoData(newBlockEto_h8.BlockHash,9, newBlockEto_h8.BlockHash);
        var newBlockEto_h10 = _mockDataHelper.MockNewBlockEtoData(10, newBlockEto_h9.BlockHash);
        
        List<NewBlockEto> newBlockEtoList_2 = new List<NewBlockEto>();
        newBlockEtoList_2.Add(newBlockEto_h6);
        newBlockEtoList_2.Add(newBlockEto_h7);
        newBlockEtoList_2.Add(newBlockEto_h8);
        await _newBlockEventHandler.HandleEventAsync(new NewBlocksEto() { NewBlocks = newBlockEtoList_2 });
        List<NewBlockEto> newBlockEtoList_3 = new List<NewBlockEto>();
        newBlockEtoList_3.Add(newBlockEto_h9);
        newBlockEtoList_3.Add(newBlockEto_h10);
        await _newBlockEventHandler.HandleEventAsync(new NewBlocksEto() { NewBlocks = newBlockEtoList_3 });
        
        // var blockIndex_h9 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h9.BlockHash)) &&
        //     q.Term(i => i.Field(f => f.BlockHeight).Value(newBlockEto_h9.BlockHeight)));
        
        // Expression<Func<BlockIndex, bool>> expression_h9 = p => p.BlockHash == newBlockEto_h9.BlockHash && p.BlockHeight == newBlockEto_h9.BlockHeight;
        // var blockIndex_h9 = queryable.Where(expression_h9).ToList();
        // var blockIndex_h9 = queryable.Where(p => p.BlockHeight == newBlockEto_h9.BlockHeight).ToList();
        var blockIndex_h9 = queryable.Where(p => p.BlockHash == newBlockEto_h9.BlockHash).ToList()[0];
        blockIndex_h9.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleEvent_NewBlock_Test12()
    {
        var newBlockEto_h11 = _mockDataHelper.MockNewBlockEtoData(11, _mockDataHelper.CreateBlockHash());
        var newBlockEto_h13 = _mockDataHelper.MockNewBlockEtoData(13, _mockDataHelper.CreateBlockHash());
        var newBlockEto_h15 = _mockDataHelper.MockNewBlockEtoData(15, _mockDataHelper.CreateBlockHash());
        
        List<NewBlockEto> newBlockEtoList_4 = new List<NewBlockEto>();
        newBlockEtoList_4.Add(newBlockEto_h11);
        newBlockEtoList_4.Add(newBlockEto_h13);
        newBlockEtoList_4.Add(newBlockEto_h15);
        await _newBlockEventHandler.HandleEventAsync(new NewBlocksEto() { NewBlocks = newBlockEtoList_4 });
    
        var queryable = await _blockIndexRepository.GetQueryableAsync();
        
        // var blockIndex_h11 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h11.BlockHash)));
        var blockIndex_h11 = queryable.Where(p => p.BlockHash == newBlockEto_h11.BlockHash).ToList()[0];
        blockIndex_h11.ShouldNotBeNull();
        // var blockIndex_h13 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h13.BlockHash)));
        var blockIndex_h13 = queryable.Where(p => p.BlockHash == newBlockEto_h13.BlockHash).ToList()[0];
        blockIndex_h13.ShouldNotBeNull();
        // var blockIndex_h15 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h15.BlockHash)));
        var blockIndex_h15 = queryable.Where(p => p.BlockHash == newBlockEto_h15.BlockHash).ToList()[0];
        blockIndex_h15.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleEvent_ConfirmBlock_Test13_14()
    {
        //Unit test 13
        var newBlockEto_h21 = _mockDataHelper.MockNewBlockEtoData(210000, _mockDataHelper.CreateBlockHash());
        var newBlockEto_h22 = _mockDataHelper.MockNewBlockEtoData(220000, newBlockEto_h21.BlockHash);
        var newBlockEto_h23 = _mockDataHelper.MockNewBlockEtoData(230000, newBlockEto_h22.BlockHash);
        var newBlockEto_h24 = _mockDataHelper.MockNewBlockEtoData(240000, newBlockEto_h23.BlockHash);
        var newBlockEto_h25 = _mockDataHelper.MockNewBlockEtoData(250000, newBlockEto_h24.BlockHash);
        var newBlockEto_h26 = _mockDataHelper.MockNewBlockEtoData(260000, newBlockEto_h25.BlockHash);
        var newBlockEto_h27 = _mockDataHelper.MockNewBlockEtoData(270000, newBlockEto_h26.BlockHash);
        var newBlockEto_h27_fork = _mockDataHelper.MockNewBlockEtoData(270000, newBlockEto_h26.BlockHash);
        var newBlockEto_h28 = _mockDataHelper.MockNewBlockEtoData(280000, newBlockEto_h27.BlockHash);
        var newBlockEto_h28_fork = _mockDataHelper.MockNewBlockEtoData(280000, newBlockEto_h27.BlockHash);
        var newBlockEto_h29 = _mockDataHelper.MockNewBlockEtoData(290000, newBlockEto_h28.BlockHash);
        var newBlockEto_h29_fork = _mockDataHelper.MockNewBlockEtoData(290000, newBlockEto_h28.BlockHash);
        var newBlockEto_h30 = _mockDataHelper.MockNewBlockEtoData(300000, newBlockEto_h29.BlockHash);
        
        List<NewBlockEto> newBlockEtoList_5 = new List<NewBlockEto>();
        newBlockEtoList_5.Add(newBlockEto_h21);
        newBlockEtoList_5.Add(newBlockEto_h22);
        newBlockEtoList_5.Add(newBlockEto_h23);
        newBlockEtoList_5.Add(newBlockEto_h24);
        newBlockEtoList_5.Add(newBlockEto_h25);
        newBlockEtoList_5.Add(newBlockEto_h26);
        newBlockEtoList_5.Add(newBlockEto_h27);
        newBlockEtoList_5.Add(newBlockEto_h27_fork);
        newBlockEtoList_5.Add(newBlockEto_h28);
        newBlockEtoList_5.Add(newBlockEto_h28_fork);
        newBlockEtoList_5.Add(newBlockEto_h29);
        newBlockEtoList_5.Add(newBlockEto_h29_fork);
        newBlockEtoList_5.Add(newBlockEto_h30);
        
        await _newBlockEventHandler.HandleEventAsync(new NewBlocksEto() { NewBlocks = newBlockEtoList_5 });
        
        var confirmBlockEto_h21 = _mockDataHelper.MockConfirmBlockEtoData(newBlockEto_h21);
        var confirmBlockEto_h22 = _mockDataHelper.MockConfirmBlockEtoData(newBlockEto_h22);
        var confirmBlockEto_h23 = _mockDataHelper.MockConfirmBlockEtoData(newBlockEto_h23);
        var confirmBlockEto_h24 = _mockDataHelper.MockConfirmBlockEtoData(newBlockEto_h24);
        var confirmBlockEto_h25 = _mockDataHelper.MockConfirmBlockEtoData(newBlockEto_h25);
        var confirmBlockEto_h26 = _mockDataHelper.MockConfirmBlockEtoData(newBlockEto_h26);
        var confirmBlockEto_h27 = _mockDataHelper.MockConfirmBlockEtoData(newBlockEto_h27);
        var confirmBlockEto_h28 = _mockDataHelper.MockConfirmBlockEtoData(newBlockEto_h28);
    
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
        
        var queryable = await _blockIndexRepository.GetQueryableAsync();
        
        // var blockIndex_h27_fork = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h27_fork.BlockHash)));
        var blockIndex_h27_fork = queryable.Where(p => p.BlockHash == newBlockEto_h27_fork.BlockHash).ToList();
        blockIndex_h27_fork.Count.ShouldBe(0);
        // var blockIndex_h28_fork = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h28_fork.BlockHash)));
        var blockIndex_h28_fork = queryable.Where(p => p.BlockHash == newBlockEto_h28_fork.BlockHash).ToList();
        blockIndex_h28_fork.Count.ShouldBe(0);
        // var blockIndex_h29_fork = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h29_fork.BlockHash)));
        var blockIndex_h29_fork = queryable.Where(p => p.BlockHash == newBlockEto_h29_fork.BlockHash).ToList()[0];
        blockIndex_h29_fork.ShouldNotBeNull();
        
        // var mustQuery = new List<Func<QueryContainerDescriptor<BlockIndex>, QueryContainer>>();
        // mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).GreaterThanOrEquals(21)));
        // mustQuery.Add(q => q.Range(i => i.Field(f => f.BlockHeight).LessThanOrEquals(28)));
        // QueryContainer Filter(QueryContainerDescriptor<BlockIndex> f) => f.Bool(b => b.Must(mustQuery));
        // var blockIndex_h21_28=await _blockIndexRepository.GetListAsync(Filter);
        Expression<Func<BlockIndex, bool>> mustQuery = p => p.BlockHeight >= 210000 && p.BlockHeight <= 280000;
        var blockIndex_h21_28 = queryable.Where(mustQuery).ToList();
        // foreach (var blockItem in blockIndex_h21_28.Item2)
        foreach (var blockItem in blockIndex_h21_28)
        {
            blockItem.Confirmed.ShouldBeTrue();
        }
        // var blockIndex_h29 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h29.BlockHash)));
        var blockIndex_h29 = queryable.Where(p => p.BlockHash == newBlockEto_h29.BlockHash).ToList()[0];
        blockIndex_h29.Confirmed.ShouldBeFalse();
        blockIndex_h29_fork.Confirmed.ShouldBeFalse();
        // var blockIndex_h30 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h30.BlockHash)));
        var blockIndex_h30 = queryable.Where(p => p.BlockHash == newBlockEto_h30.BlockHash).ToList()[0];
        blockIndex_h30.Confirmed.ShouldBeFalse();
        
        
        //Unit test 14
        var confirmBlockEto_h31 = _mockDataHelper.MockConfirmBlockEtoData(_mockDataHelper.CreateBlockHash(), 31);
        ConfirmBlocksEto confirmBlocks = new ConfirmBlocksEto()
        {
            ConfirmBlocks = new List<ConfirmBlockEto>(){confirmBlockEto_h31}
        };
        
        await _confirmBlockEventHandler.HandleEventAsync(confirmBlocks);
        
        // blockIndex_h30 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(newBlockEto_h30.BlockHash)));
        blockIndex_h30 = queryable.Where(p => p.BlockHash == newBlockEto_h30.BlockHash).ToList()[0];
        blockIndex_h30.Confirmed.ShouldBeFalse();
        
        // var blockIndex_h31 = await _blockIndexRepository.GetAsync(q =>
        //     q.Term(i => i.Field(f => f.BlockHash).Value(confirmBlockEto_h31.BlockHash)));
        // blockIndex_h31.ShouldBeNull();
    }
    
}