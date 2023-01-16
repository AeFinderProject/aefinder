using System;
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
using Xunit;

namespace AElfIndexer.Handlers;

public class BlockHandlerTests : AElfIndexerClientTestBase
{
    private readonly IBlockChainDataHandler _blockChainDataHandler;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> _repository;
    private readonly IClusterClient _clusterClient;

    public BlockHandlerTests()
    {
        _blockChainDataHandler = GetRequiredService<IBlockChainDataHandler>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _repository = GetRequiredService<IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo>>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }

    [Fact]
    public async Task Block_Linked_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var blocks = CreateBlock(100, 10, "BlockHash", chainId);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(10);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash100");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash109");

        var grain =
            _clusterClient.GetGrain<IBlockStateSetsGrain<BlockInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSet();
        bestChainBlockStateSet.BlockHash.ShouldBe("BlockHash109");
        bestChainBlockStateSet.BlockHeight.ShouldBe(109);
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
    }

    [Fact]
    public async Task Block_Fork_Branch_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();

        var firstBlock = CreateBlock(99, 1, "BlockHash", chainId);
        var blocks = CreateBlock(100, 5, "BlockHash", chainId, "BlockHash99");
        var blocksForkBlock = CreateBlock(100, 2, "BlockForkHash", chainId, "BlockHash99");
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

        var firstBlock = CreateBlock(99, 1, "BlockHash", chainId);
        var blocks = CreateBlock(100, 5, "BlockHash", chainId, "BlockHash99");
        var blocksForkBlock = CreateBlock(100, 2, "BlockForkHash", chainId, "BlockHash99");
        firstBlock.AddRange(blocksForkBlock);
        firstBlock.AddRange(blocks);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, firstBlock);

        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(3);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockForkHash101");
        
        blocks = CreateBlock(104, 5, "BlockHash", chainId);
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

        var firstBlock = CreateBlock(99, 1, "BlockHash", chainId);
        var blocks = CreateBlock(100, 3, "BlockHash", chainId, "BlockHash99");
        blocks.Insert(0, firstBlock.First());

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);
        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(4);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash102");

        var blocksForkBlock = CreateBlock(100, 4, "BlockForkHash", chainId, "BlockHash99");
        blocksForkBlock.Insert(0, firstBlock.First());

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocksForkBlock);
        blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(5);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockForkHash103");

        blocks = CreateBlock(103, 3, "BlockHash", chainId);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(7);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash105");

        var grain =
            _clusterClient.GetGrain<IBlockStateSetsGrain<BlockInfo>>(
                GrainIdHelper.GenerateGrainId("BlockStateSets", client, chainId, version));
        var bestChainBlockStateSet = await grain.GetBestChainBlockStateSet();
        bestChainBlockStateSet.Confirmed.ShouldBeFalse();
        bestChainBlockStateSet.Processed.ShouldBeTrue();
        bestChainBlockStateSet.BlockHeight.ShouldBe(105);
    }

    [Fact]
    public async Task Block_NotLined_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();

        var blocks = CreateBlock(98, 1, "BlockHash", chainId);
        var blocksForkBlock = CreateBlock(100, 1, "BlockHash", chainId);
        blocks.AddRange(blocksForkBlock);

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocks);

        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(1);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash98");
    }

    [Fact]
    public async Task LIB_Height_Set_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var blocksForkBlocks = CreateBlock(100, 10, "BlockHash", chainId, "BlockHash99");
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocksForkBlocks);
        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(10);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash100");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash109");

        var confirmedBlock = CreateBlock(100, 10, "BlockHash", chainId, "BlockHash99", true);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, confirmedBlock);

        var grain = _clusterClient.GetGrain<IBlockStateSetInfoGrain>(
            GrainIdHelper.GenerateGrainId("BlockStateSetInfo", client, chainId, client, version));
        
        var confirmedBlockHeight = await grain.GetConfirmedBlockHeight(BlockFilterType.Block);
        confirmedBlockHeight.ShouldBe(109);
    }

    [Fact]
    public async Task Fork_Branch_LIB_Height_Set_Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();
        var version = _clientInfoProvider.GetVersion();

        var firstBlock = CreateBlock(99, 1, "BlockHash", chainId);
        var blocksForkBlocks = CreateBlock(100, 10, "BlockForkHash", chainId, "BlockHash99");
        blocksForkBlocks.Insert(0, firstBlock.First());

        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, blocksForkBlocks);
        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(11);
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockForkHash109");

        var confirmedBlock = 
            CreateBlock(100, 1, "BlockHash", chainId, "BlockHash99", true);
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, confirmedBlock);

        var block = 
            CreateBlock(101, 10, "BlockHash", chainId, "BlockHash100");
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId, client, block);
        blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Exists(i => i.BlockHash.Equals("BlockHash100")).ShouldBeTrue();
        blockIndexes.Item2.First().BlockHash.ShouldBe("BlockHash99");
        blockIndexes.Item2.Last().BlockHash.ShouldBe("BlockHash110");

        var grain = _clusterClient.GetGrain<IBlockStateSetInfoGrain>(
            GrainIdHelper.GenerateGrainId("BlockStateSetInfo", client, chainId, client, version));
        var confirmedBlockHeight = await grain.GetConfirmedBlockHeight(BlockFilterType.Block);
        confirmedBlockHeight.ShouldBe(100);
    }

    private List<BlockWithTransactionDto> CreateBlock(long startBlock, long blockCount, string blockHash,
        string chainId, string perHash = "", bool confirmed = false)
    {
        var blocks = new List<BlockWithTransactionDto>();
        for (var i = startBlock; i < startBlock + blockCount; i++)
        {
            var perBlockHash = i == startBlock && perHash != "" ? perHash : blockHash + (i - 1);
            var blockWithTransactionDto = new BlockWithTransactionDto
            {
                Id = blockHash + i,
                Confirmed = confirmed,
                BlockHash = blockHash + i,
                BlockHeight = i,
                BlockTime = DateTime.UtcNow.AddMilliseconds(i),
                ChainId = chainId,
                PreviousBlockHash = perBlockHash
            };
            blocks.Add(blockWithTransactionDto);
        }

        return blocks;
    }
}