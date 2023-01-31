using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using Orleans;
using Shouldly;
using Volo.Abp.ObjectMapping;
using Xunit;

namespace AElfIndexer.Handler;

public class BlockHandlerTests : AElfIndexerClientBlockDataHandlerTestBase
{
    private readonly IBlockChainDataHandler _blockChainDataHandler;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> _repository;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IBlockStateSetProvider<BlockInfo> _blockStateSetProvider;

    public BlockHandlerTests()
    {
        _blockChainDataHandler = GetRequiredService<IBlockChainDataHandler>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _repository = GetRequiredService<IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo>>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _objectMapper = GetRequiredService<IObjectMapper>();
        _blockStateSetProvider = GetRequiredService<IBlockStateSetProvider<BlockInfo>>();
    }

    [Fact]
    public async Task Block_Linked_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var blocks = MockHandlerHelper.CreateBlock(100, 10, "BlockHash", chainId);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(10);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash100");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash109");

        var grain =
            _clusterClient.GetGrain<IBlockStateSetGrain<BlockInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSetAsync();
        bestChainBlockStateSet.BlockHash.ShouldBe("BlockHash109");
        bestChainBlockStateSet.BlockHeight.ShouldBe(109);
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
    }

    [Fact]
    public async Task Block_NotLined_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();

        var blocks = MockHandlerHelper.CreateBlock(98, 1, "BlockHash", chainId);
        var blocksForkBlock = MockHandlerHelper.CreateBlock(100, 1, "BlockHash", chainId);
        blocks.AddRange(blocksForkBlock);

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(1);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash98");
    }

    [Fact]
    public async Task Block_Fork_Branch_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();

        var firstBlock = MockHandlerHelper.CreateBlock(99, 1, "BlockHash", chainId);
        var blocks = MockHandlerHelper.CreateBlock(100, 5, "BlockHash", chainId, "BlockHash99");
        var blocksForkBlock = MockHandlerHelper.CreateBlock(100, 2, "BlockForkHash", chainId, "BlockHash99");
        firstBlock.AddRange(blocks);
        firstBlock.AddRange(blocksForkBlock);

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, firstBlock);

        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(6);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash104");
    }

    [Fact]
    public async Task Block_Fork_Branch_Test_2()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();

        var firstBlock = MockHandlerHelper.CreateBlock(99, 1, "BlockHash", chainId);
        var blocks = MockHandlerHelper.CreateBlock(100, 5, "BlockHash", chainId, "BlockHash99");
        var blocksForkBlock = MockHandlerHelper.CreateBlock(100, 2, "BlockForkHash", chainId, "BlockHash99");
        firstBlock.AddRange(blocksForkBlock);
        firstBlock.AddRange(blocks);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, firstBlock);

        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(3);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockForkHash101");

        blocks = MockHandlerHelper.CreateBlock(104, 5, "BlockHash", chainId);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);
        blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(10);
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash108");
    }

    [Fact]
    public async Task Block_Set_Longest_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var firstBlock = MockHandlerHelper.CreateBlock(99, 1, "BlockHash", chainId);
        var blocks = MockHandlerHelper.CreateBlock(100, 3, "BlockHash", chainId, "BlockHash99");
        blocks.Insert(0, firstBlock.First());

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);
        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(4);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash102");

        var blocksForkBlock = MockHandlerHelper.CreateBlock(100, 4, "BlockForkHash", chainId, "BlockHash99");
        blocksForkBlock.Insert(0, firstBlock.First());

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocksForkBlock);
        blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(5);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockForkHash103");

        blocks = MockHandlerHelper.CreateBlock(103, 3, "BlockHash", chainId);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(7);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash105");

        var grain =
            _clusterClient.GetGrain<IBlockStateSetGrain<BlockInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSetAsync();
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
        bestChainBlockStateSet.BlockHeight.ShouldBe(105);
    }

    [Fact]
    public async Task LIB_Height_Set_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var blocksForkBlocks = MockHandlerHelper.CreateBlock(100, 10, "BlockHash", chainId, "BlockHash99");
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocksForkBlocks);
        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(10);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash100");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash109");
        
        var blockStateSerGrain =
            _clusterClient.GetGrain<IBlockStateSetGrain<BlockInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var blockStateSet = await blockStateSerGrain.GetBlockStateSetsAsync();
        blockStateSet.Count.ShouldBe(10);


        var confirmedBlock = MockHandlerHelper.CreateBlock(100, 5, "BlockHash", chainId, "BlockHash99", true);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, confirmedBlock);

        var grain = _clusterClient.GetGrain<IBlockStateSetInfoGrain>(
            GrainIdHelper.GenerateGrainId("BlockStateSetInfo", client, chainId, version));

        var confirmedBlockHeight = await grain.GetConfirmedBlockHeight(BlockFilterType.Block);
        confirmedBlockHeight.ShouldBe(104);
        
        blockStateSet = await blockStateSerGrain.GetBlockStateSetsAsync();
        blockStateSet.Count.ShouldBe(6);
        blockStateSet.Keys.ShouldNotContain("BlockHash100");
        blockStateSet.Keys.ShouldNotContain("BlockHash101");
        blockStateSet.Keys.ShouldNotContain("BlockHash102");
        blockStateSet.Keys.ShouldNotContain("BlockHash103");
    }

    [Fact]
    public async Task Fork_Branch_LIB_Height_Set_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var firstBlock = MockHandlerHelper.CreateBlock(99, 1, "BlockHash", chainId);
        var blocksForkBlocks = MockHandlerHelper.CreateBlock(100, 10, "BlockForkHash", chainId, "BlockHash99");
        blocksForkBlocks.Insert(0, firstBlock.First());

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocksForkBlocks);
        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(11);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockForkHash109");

        var blockStateSerGrain =
            _clusterClient.GetGrain<IBlockStateSetGrain<BlockInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var blockStateSet = await blockStateSerGrain.GetBlockStateSetsAsync();
        blockStateSet.Keys.Count.ShouldBe(11);
        blockStateSet.Keys.ShouldContain("BlockHash99");
        blockStateSet.Keys.ShouldContain("BlockForkHash100");

        
        var confirmedBlock =
            MockHandlerHelper.CreateBlock(100, 1, "BlockHash", chainId, "BlockHash99", true);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, confirmedBlock);

        var block =
            MockHandlerHelper.CreateBlock(101, 10, "BlockHash", chainId, "BlockHash100");
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, block);
        blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Exists(i => i.BlockHash.Equals("BlockHash100")).ShouldBeTrue();
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash110");

        var grain = _clusterClient.GetGrain<IBlockStateSetInfoGrain>(
            GrainIdHelper.GenerateGrainId("BlockStateSetInfo", client, chainId, version));
        var confirmedBlockHeight = await grain.GetConfirmedBlockHeight(BlockFilterType.Block);
        confirmedBlockHeight.ShouldBe(100);
        
        blockStateSet = await blockStateSerGrain.GetBlockStateSetsAsync();
        blockStateSet.Keys.Count.ShouldBe(20);
        blockStateSet.Keys.ShouldNotContain("BlockHash99");
        blockStateSet.Keys.ShouldNotContain("BlockForkHash100");
    }

    [Fact]
    public async Task Unfinished_Longest_Chain_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();
        var stateSetKey = GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version);

        var blocks = MockHandlerHelper.CreateBlock(100, 10, "BlockHash", chainId);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);
        
        blocks = MockHandlerHelper.CreateBlock(110, 10, "BlockHash", chainId);
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

        await _blockStateSetProvider.SetLongestChainBlockStateSetAsync(stateSetKey, sets.Last().BlockHash);
        
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(20);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash100");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash119");

        var grain = _clusterClient.GetGrain<IBlockStateSetGrain<BlockInfo>>(stateSetKey);
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSetAsync();
        bestChainBlockStateSet.BlockHash.ShouldBe("BlockHash119");
        bestChainBlockStateSet.BlockHeight.ShouldBe(119);
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
    }
}