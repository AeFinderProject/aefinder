using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using Shouldly;
using Xunit;

namespace AElfIndexer.Handlers;

public class BlockHandlerTests : AElfIndexerClientTestBase
{
    private readonly IBlockChainDataHandler _blockChainDataHandler;
    private readonly IAElfIndexerClientInfoProvider _clientInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo> _repository;

    public BlockHandlerTests()
    {
        _blockChainDataHandler = GetRequiredService<IBlockChainDataHandler>();
        _clientInfoProvider = GetRequiredService<IAElfIndexerClientInfoProvider>();
        _repository = GetRequiredService<IAElfIndexerClientEntityRepository<TestBlockIndex, BlockInfo>>();
    }

    [Fact]
    public async Task Test()
    {
        var chainId = "AELF";
        var client = _clientInfoProvider.GetClientId();

        var blocks = new List<BlockWithTransactionDto>
        {
            new BlockWithTransactionDto
            {
                Id = "Block100",
                Confirmed = false,
                BlockHash = "Block100",
                BlockHeight = 100,
                BlockTime = DateTime.UtcNow,
                ChainId = chainId,
                PreviousBlockHash = "Block99"
            }
        };
        
        await _blockChainDataHandler.HandleBlockChainDataAsync(chainId,client,blocks);
        
        var blockIndexes = await _repository.GetListAsync();
        blockIndexes.Item2.Count.ShouldBe(1);
        blockIndexes.Item2[0].BlockHash.ShouldBe("Block100");
    }
}