using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.State.Client;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace AElfIndexer.Client;

public class AElfIndexerClientEntityRepositoryTests : AElfIndexerClientTestBase
{
    private readonly IAElfIndexerClientEntityRepository<TestIndex, BlockInfo> _clientEntityRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockStateSetProvider<BlockInfo> _blockStateSetProvider;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IDAppDataIndexProvider<TestIndex> _dAppDataIndexProvider;
    private readonly IDAppDataProvider _dAppDataProvider;
    
    public AElfIndexerClientEntityRepositoryTests()
    {
        _clientEntityRepository = GetRequiredService<IAElfIndexerClientEntityRepository<TestIndex, BlockInfo>>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _blockStateSetProvider = GetRequiredService<IBlockStateSetProvider<BlockInfo>>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _dAppDataIndexProvider = GetRequiredService<IDAppDataIndexProvider<TestIndex>>();
        _dAppDataProvider = GetRequiredService<IDAppDataProvider>();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddAndDelete_Test(bool isConfirmed)
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();
        var stateSetKey = GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version);

        var blocks = MockHandlerHelper.CreateBlock(100, 10, "BlockHash", chainId, confirmed: isConfirmed);
        var sets = new List<BlockStateSet<BlockInfo>>();
        foreach (var block in blocks)
        {
            var set = new BlockStateSet<BlockInfo>
            {
                BlockHash = block.BlockHash,
                BlockHeight = block.BlockHeight,
                PreviousBlockHash = block.PreviousBlockHash,
                Confirmed = block.Confirmed,
                Data = new List<BlockInfo> { _objectMapper.Map<BlockWithTransactionDto, BlockInfo>(block) },
                Changes = new()
            };
            sets.Add(set);
            await _blockStateSetProvider.SetBlockStateSetAsync(stateSetKey, set);
        }
        await _blockStateSetProvider.SetLongestChainHashesAsync(stateSetKey,
            sets.ToDictionary(o => o.BlockHash, o => o.PreviousBlockHash));
        await _blockStateSetProvider.SetLongestChainBlockStateSetAsync(stateSetKey, sets.Last().BlockHash);
        
        await _blockStateSetProvider.SetCurrentBlockStateSetAsync(stateSetKey, sets[0]);
        
        var entity = _objectMapper.Map<BlockInfo, TestIndex>(sets[0].Data[0]);
        entity.Id = "Entity1";
        entity.Value = 100;
        await _clientEntityRepository.AddOrUpdateAsync(entity);

        var data = await _clientEntityRepository.GetFromBlockStateSetAsync(entity.Id, chainId);
        data.Id.ShouldBe(entity.Id);
        data.Value.ShouldBe(entity.Value);
        
        await _blockStateSetProvider.SetCurrentBlockStateSetAsync(stateSetKey, sets[1]);
        
        entity = _objectMapper.Map<BlockInfo, TestIndex>(sets[1].Data[0]);
        entity.Id = "Entity1";
        entity.Value = 101;
        await _clientEntityRepository.AddOrUpdateAsync(entity);

        data = await _clientEntityRepository.GetFromBlockStateSetAsync(entity.Id, chainId);
        data.Id.ShouldBe(entity.Id);
        data.Value.ShouldBe(entity.Value);
        
        await _blockStateSetProvider.SetCurrentBlockStateSetAsync(stateSetKey, sets[2]);
        await _clientEntityRepository.DeleteAsync(entity);
        
        data = await _clientEntityRepository.GetFromBlockStateSetAsync(entity.Id, chainId);
        data.ShouldBeNull();
    }

    [Fact]
    public async Task Add_Fork_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();
        var stateSetKey = GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version);

        var blocks = MockHandlerHelper.CreateBlock(100, 10, "BlockHash", chainId);
        var sets = new List<BlockStateSet<BlockInfo>>();
        foreach (var block in blocks)
        {
            var set = new BlockStateSet<BlockInfo>
            {
                BlockHash = block.BlockHash,
                BlockHeight = block.BlockHeight,
                PreviousBlockHash = block.PreviousBlockHash,
                Confirmed = block.Confirmed,
                Data = new List<BlockInfo> { _objectMapper.Map<BlockWithTransactionDto, BlockInfo>(block) },
                Changes = new()
            };
            sets.Add(set);
            await _blockStateSetProvider.SetBlockStateSetAsync(stateSetKey, set);
        }
        await _blockStateSetProvider.SetLongestChainHashesAsync(stateSetKey,
            sets.ToDictionary(o => o.BlockHash, o => o.PreviousBlockHash));
        await _blockStateSetProvider.SetLongestChainBlockStateSetAsync(stateSetKey, sets.Last().BlockHash);
        
        await _blockStateSetProvider.SetCurrentBlockStateSetAsync(stateSetKey, sets[2]);
        
        var entity = _objectMapper.Map<BlockInfo, TestIndex>(sets[2].Data[0]);
        entity.Id = "Entity1";
        entity.Value = 100;
        await _clientEntityRepository.AddOrUpdateAsync(entity);
        
        var newBranchBlocks = MockHandlerHelper.CreateBlock(102, 10, "NewBranchBlockHash", chainId, "BlockHash101");
        var newBranchSets = new List<BlockStateSet<BlockInfo>>();
        foreach (var block in newBranchBlocks)
        {
            var set = new BlockStateSet<BlockInfo>
            {
                BlockHash = block.BlockHash,
                BlockHeight = block.BlockHeight,
                PreviousBlockHash = block.PreviousBlockHash,
                Confirmed = block.Confirmed,
                Data = new List<BlockInfo> { _objectMapper.Map<BlockWithTransactionDto, BlockInfo>(block) },
                Changes = new()
            };
            newBranchSets.Add(set);
            await _blockStateSetProvider.SetBlockStateSetAsync(stateSetKey, set);
        }
        await _blockStateSetProvider.SetLongestChainHashesAsync(stateSetKey,
            newBranchSets.ToDictionary(o => o.BlockHash, o => o.PreviousBlockHash));
        await _blockStateSetProvider.SetLongestChainBlockStateSetAsync(stateSetKey, newBranchSets.Last().BlockHash);
        
        await _blockStateSetProvider.SetCurrentBlockStateSetAsync(stateSetKey, sets[2]);
        await _clientEntityRepository.AddOrUpdateAsync(entity);

        await _blockStateSetProvider.SetCurrentBlockStateSetAsync(stateSetKey, newBranchSets[0]);
        var newBranchEntity = _objectMapper.Map<BlockInfo, TestIndex>(newBranchSets[0].Data[0]);
        newBranchEntity.Id = "Entity1";
        newBranchEntity.Value = 101;
        await _clientEntityRepository.AddOrUpdateAsync(newBranchEntity);
        
        var data = await _clientEntityRepository.GetFromBlockStateSetAsync(newBranchEntity.Id, chainId);
        data.Value.ShouldBe(newBranchEntity.Value);
        
        await _blockStateSetProvider.SetCurrentBlockStateSetAsync(stateSetKey, newBranchSets[1]);
        newBranchEntity = _objectMapper.Map<BlockInfo, TestIndex>(newBranchSets[1].Data[0]);
        newBranchEntity.Id = "Entity1";
        newBranchEntity.Value = 102;
        await _clientEntityRepository.AddOrUpdateAsync(newBranchEntity);
        
        data = await _clientEntityRepository.GetFromBlockStateSetAsync(newBranchEntity.Id, chainId);
        data.Value.ShouldBe(newBranchEntity.Value);
        
        blocks = MockHandlerHelper.CreateBlock(110, 5, "BlockHash", chainId,"BlockHash109");
        sets = new List<BlockStateSet<BlockInfo>>();
        foreach (var block in blocks)
        {
            var set = new BlockStateSet<BlockInfo>
            {
                BlockHash = block.BlockHash,
                BlockHeight = block.BlockHeight,
                PreviousBlockHash = block.PreviousBlockHash,
                Confirmed = block.Confirmed,
                Data = new List<BlockInfo> { _objectMapper.Map<BlockWithTransactionDto, BlockInfo>(block) },
                Changes = new()
            };
            sets.Add(set);
            await _blockStateSetProvider.SetBlockStateSetAsync(stateSetKey, set);
        }
        await _blockStateSetProvider.SetLongestChainHashesAsync(stateSetKey,
            sets.ToDictionary(o => o.BlockHash, o => o.PreviousBlockHash));
        await _blockStateSetProvider.SetLongestChainBlockStateSetAsync(stateSetKey, sets.Last().BlockHash);
        
        await _blockStateSetProvider.SetCurrentBlockStateSetAsync(stateSetKey, newBranchSets[1]);
        await _clientEntityRepository.AddOrUpdateAsync(newBranchEntity);
        
        await _blockStateSetProvider.SetCurrentBlockStateSetAsync(stateSetKey, sets[0]);
        
        entity = _objectMapper.Map<BlockInfo, TestIndex>(sets[0].Data[0]);
        entity.Id = "Entity1";
        entity.Value = 103;
        await _clientEntityRepository.AddOrUpdateAsync(entity);
        
        data = await _clientEntityRepository.GetFromBlockStateSetAsync(entity.Id, chainId);
        data.Value.ShouldBe(entity.Value);
    }

    [Fact]
    public async Task Query_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();
        var stateSetKey = GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version);
        
        var blocks = MockHandlerHelper.CreateBlock(100, 10, "BlockHash", chainId);
        var sets = new List<BlockStateSet<BlockInfo>>();
        foreach (var block in blocks)
        {
            var set = new BlockStateSet<BlockInfo>
            {
                BlockHash = block.BlockHash,
                BlockHeight = block.BlockHeight,
                PreviousBlockHash = block.PreviousBlockHash,
                Confirmed = block.Confirmed,
                Data = new List<BlockInfo> { _objectMapper.Map<BlockWithTransactionDto, BlockInfo>(block) },
                Changes = new()
            };
            sets.Add(set);
            await _blockStateSetProvider.SetBlockStateSetAsync(stateSetKey, set);
        }

        await _blockStateSetProvider.SetLongestChainHashesAsync(stateSetKey,
            sets.ToDictionary(o => o.BlockHash, o => o.PreviousBlockHash));
        await _blockStateSetProvider.SetLongestChainBlockStateSetAsync(stateSetKey, sets.Last().BlockHash);

        var entity = _objectMapper.Map<BlockInfo, TestIndex>(sets[0].Data[0]);
        entity.Id = "Entity1";
        entity.Value = 100;
        await _clientEntityRepository.AddOrUpdateAsync(entity);
        await _dAppDataIndexProvider.SaveDataAsync();

        var index = await _clientEntityRepository.GetAsync(entity.Id);
        index.Id.ShouldBe(entity.Id);
        index.Value.ShouldBe(entity.Value);

        index = await _clientEntityRepository.GetAsync(q =>
            q.Bool(b => b.Must(o => o.Term(i => i.Field(f => f.BlockHash).Value(entity.BlockHash)))));
        index.Id.ShouldBe(entity.Id);

        var indexes = await _clientEntityRepository.GetListAsync();
        indexes.Item2.Count.ShouldBe(1);
        indexes.Item2[0].Id.ShouldBe(entity.Id);
        
        indexes = await _clientEntityRepository.GetSortListAsync();
        indexes.Item2.Count.ShouldBe(1);
        indexes.Item2[0].Id.ShouldBe(entity.Id);
        
        var count = await _clientEntityRepository.CountAsync(q =>
            q.Bool(b => b.Must(o => o.Term(i => i.Field(f => f.BlockHash).Value(entity.BlockHash)))));
        count.Count.ShouldBe(1);
    }
}