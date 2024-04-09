using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Block.Dtos;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Subscriptions;
using Force.DeepCloner;
using Shouldly;
using Xunit;

namespace AeFinder.Grains.BlockPush;

public class BlockFilterProviderTests : AeFinderGrainTestBase
{
    private readonly IBlockFilterProvider _blockFilterProvider;
    private readonly IBlockDataProvider _blockDataProvider;

    public BlockFilterProviderTests()
    {
        _blockFilterProvider = GetRequiredService<IBlockFilterProvider>();
        _blockDataProvider = GetRequiredService<IBlockDataProvider>();
    }

    [Fact]
    public async Task FilterBlocks_Test()
    {
        var block = MockBlock();
        var filter = new List<LogEventCondition>
        {
            new LogEventCondition
            {
                ContractAddress = "ContractAddress",
                EventNames = new List<string> { "EventName" }
            }
        };

        var filterOnlyContractAddress = new List<LogEventCondition>
        {
            new LogEventCondition
            {
                ContractAddress = "ContractAddress2"
            }
        };

        var filterNotExist = new List<LogEventCondition>
        {
            new LogEventCondition
            {
                ContractAddress = "ContractAddress2",
                EventNames = new List<string> { "EventName" }
            }
        };

        var filteredBlock =
            await _blockFilterProvider.FilterBlocksAsync(new List<BlockWithTransactionDto> { block }, null, null);
        filteredBlock.Count.ShouldBe(1);

        filteredBlock = await _blockFilterProvider.FilterBlocksAsync(new List<BlockWithTransactionDto> { block },
            new List<TransactionCondition>(), new List<LogEventCondition>());
        filteredBlock.Count.ShouldBe(1);

        filteredBlock =
            await _blockFilterProvider.FilterBlocksAsync(new List<BlockWithTransactionDto> { block }, null, filter);
        filteredBlock.Count.ShouldBe(1);

        filteredBlock = await _blockFilterProvider.FilterBlocksAsync(new List<BlockWithTransactionDto> { block }, null,
            filterOnlyContractAddress);
        filteredBlock.Count.ShouldBe(1);

        filteredBlock =
            await _blockFilterProvider.FilterBlocksAsync(new List<BlockWithTransactionDto> { block }, null, filterNotExist);
        filteredBlock.Count.ShouldBe(1);
    }

    [Fact]
    public async Task FilterIncompleteBlocks_Unlinked_Test()
    {
        var blocks = new List<BlockWithTransactionDto>();
        blocks.Add(_blockDataProvider.Blocks[21][0]);
        blocks.Add(_blockDataProvider.Blocks[22][0]);
        blocks.Add(_blockDataProvider.Blocks[24][0]);


        var filteredBlock = await _blockFilterProvider.FilterIncompleteConfirmedBlocksAsync("AELF", blocks,
            _blockDataProvider.Blocks[20][0].BlockHash, _blockDataProvider.Blocks[20][0].BlockHeight);
        filteredBlock.Count.ShouldBe(2);

        filteredBlock = await _blockFilterProvider.FilterIncompleteBlocksAsync("AELF", blocks);
        filteredBlock.Count.ShouldBe(3);
    }

    [Fact]
    public async Task FilterIncompleteBlocks_WrongBlock_Test()
    {
        var blocks = new List<BlockWithTransactionDto>();
        blocks.Add(_blockDataProvider.Blocks[21][0]);
        blocks.Add(_blockDataProvider.Blocks[22][0]);
        var block = MockBlock();
        block.PreviousBlockHash = _blockDataProvider.Blocks[22][0].BlockHash;
        block.BlockHeight = _blockDataProvider.Blocks[22][0].BlockHeight + 1;
        blocks.Add(block);
        blocks.Add(_blockDataProvider.Blocks[24][0]);

        var filteredBlock = await _blockFilterProvider.FilterIncompleteConfirmedBlocksAsync("AELF", blocks,
            _blockDataProvider.Blocks[20][0].BlockHash, _blockDataProvider.Blocks[20][0].BlockHeight);
        filteredBlock.Count.ShouldBe(2);

        filteredBlock = await _blockFilterProvider.FilterIncompleteBlocksAsync("AELF", blocks);
        filteredBlock.Count.ShouldBe(2);
    }

    [Fact]
    public async Task FilterIncompleteBlocks_WrongLogEvent_Test()
    {
        var blocks = new List<BlockWithTransactionDto>();
        blocks.Add(_blockDataProvider.Blocks[21][0]);
        blocks.Add(_blockDataProvider.Blocks[22][0]);
        var block = _blockDataProvider.Blocks[23][0].DeepClone();
        block.Transactions[0].LogEvents.Add(new LogEventDto());
        blocks.Add(block);
        blocks.Add(_blockDataProvider.Blocks[24][0]);

        var filteredBlock = await _blockFilterProvider.FilterIncompleteConfirmedBlocksAsync("AELF", blocks,
            _blockDataProvider.Blocks[20][0].BlockHash, _blockDataProvider.Blocks[20][0].BlockHeight);
        filteredBlock.Count.ShouldBe(4);

        filteredBlock = await _blockFilterProvider.FilterIncompleteBlocksAsync("AELF", blocks);
        filteredBlock.Count.ShouldBe(4);
    }

    [Fact]
    public async Task GetBlocks_MissingBlock_Test()
    {
        await Assert.ThrowsAsync<ApplicationException>(async () =>
            await _blockFilterProvider.GetBlocksAsync(new GetSubscriptionTransactionsInput
            {
                ChainId = "AELF",
                StartBlockHeight = 0,
                EndBlockHeight = 10
            }));
    }

    private BlockWithTransactionDto MockBlock()
    {
        var chainId = "AELF";
        var blockNum = 1000;
        
        var blockHash = "BlockHash";
        return  new BlockWithTransactionDto
        {
            ChainId = chainId,
            BlockHash = blockHash,
            BlockHeight = blockNum,
            Confirmed = true,
            BlockTime = DateTime.UtcNow,
            PreviousBlockHash = "PreviousHash",
            Transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    ChainId = chainId,
                    TransactionId = Guid.NewGuid().ToString(),
                    BlockHash = blockHash,
                    BlockHeight = blockNum,
                    Confirmed = true,
                    BlockTime = DateTime.UtcNow,
                    PreviousBlockHash = "PreviousHash",
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = Guid.NewGuid().ToString(),
                            BlockHash = blockHash,
                            BlockHeight = blockNum,
                            Confirmed = true,
                            BlockTime = DateTime.UtcNow,
                            PreviousBlockHash = "PreviousHash",
                            ContractAddress = "ContractAddress",
                            EventName = "EventName"
                        },
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = Guid.NewGuid().ToString(),
                            BlockHash = blockHash,
                            BlockHeight = blockNum,
                            Confirmed = true,
                            BlockTime = DateTime.UtcNow,
                            PreviousBlockHash = "PreviousHash",
                            ContractAddress = "ContractAddress",
                            EventName = "EventName"
                        },
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = Guid.NewGuid().ToString(),
                            BlockHash = blockHash,
                            BlockHeight = blockNum,
                            Confirmed = true,
                            BlockTime = DateTime.UtcNow,
                            PreviousBlockHash = "PreviousHash",
                            ContractAddress = "ContractAddress2",
                            EventName = "EventName2"
                        }
                    }
                },
                new TransactionDto
                {
                    ChainId = chainId,
                    TransactionId = Guid.NewGuid().ToString(),
                    BlockHash = blockHash,
                    BlockHeight = blockNum,
                    Confirmed = true,
                    BlockTime = DateTime.UtcNow,
                    PreviousBlockHash = "PreviousHash",
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = Guid.NewGuid().ToString(),
                            BlockHash = blockHash,
                            BlockHeight = blockNum,
                            Confirmed = true,
                            BlockTime = DateTime.UtcNow,
                            PreviousBlockHash = "PreviousHash",
                            ContractAddress = "ContractAddress",
                            EventName = "EventName"
                        },
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = Guid.NewGuid().ToString(),
                            BlockHash = blockHash,
                            BlockHeight = blockNum,
                            Confirmed = true,
                            BlockTime = DateTime.UtcNow,
                            PreviousBlockHash = "PreviousHash",
                            ContractAddress = "ContractAddress",
                            EventName = "EventName"
                        }
                        
                    }
                }
            }
        };
    }
}