using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScan.Block.Dtos;
using AElfScan.Grains.Grain.BlockScan;
using Shouldly;
using Xunit;

namespace AElfScan.Grains.BlockScan;

public class BlockFilterProviderTests : AElfScanGrainTestBase
{
    private readonly IEnumerable<IBlockFilterProvider> _blockFilterProviders;

    public BlockFilterProviderTests()
    {
        _blockFilterProviders = GetRequiredService<IEnumerable<IBlockFilterProvider>>();
    }

    [Fact]
    public async Task FilterBlocksTest()
    {
        var block = MockBlock();
        var filter = new List<FilterContractEventInput>
        {
            new FilterContractEventInput
            {
                ContractAddress = "ContractAddress",
                EventNames = new List<string> { "EventName" }
            }
        };
        
        var filterOnlyContractAddress = new List<FilterContractEventInput>
        {
            new FilterContractEventInput
            {
                ContractAddress = "ContractAddress2"
            }
        };
        
        var filterNotExist = new List<FilterContractEventInput>
        {
            new FilterContractEventInput
            {
                ContractAddress = "ContractAddress2",
                EventNames = new List<string> { "EventName" }
            }
        };

        {
            var blockFilterProvider = _blockFilterProviders.First(o => o.FilterType == BlockFilterType.Block);

            var filteredBlock = await blockFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, null);
            filteredBlock.Count.ShouldBe(1);

            filteredBlock = await blockFilterProvider.FilterBlocksAsync(new List<BlockDto> { block },
                new List<FilterContractEventInput>());
            filteredBlock.Count.ShouldBe(1);

            filteredBlock = await blockFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filter);
            filteredBlock.Count.ShouldBe(1);
            filteredBlock[0].Transactions.Count().ShouldBe(2);
            filteredBlock[0].Transactions[0].LogEvents.Count().ShouldBe(3);
            filteredBlock[0].Transactions[1].LogEvents.Count().ShouldBe(2);
            
            filteredBlock = await blockFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filterOnlyContractAddress);
            filteredBlock.Count.ShouldBe(1);
            filteredBlock[0].Transactions.Count().ShouldBe(2);
            filteredBlock[0].Transactions[0].LogEvents.Count().ShouldBe(3);
            filteredBlock[0].Transactions[1].LogEvents.Count().ShouldBe(2);

            filteredBlock = await blockFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filterNotExist);
            filteredBlock.Count.ShouldBe(0);
        }

        {
            var transactionFilterProvider =
                _blockFilterProviders.First(o => o.FilterType == BlockFilterType.Transaction);

            var filteredBlock = await transactionFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, null);
            filteredBlock.Count.ShouldBe(1);

            filteredBlock = await transactionFilterProvider.FilterBlocksAsync(new List<BlockDto> { block },
                new List<FilterContractEventInput>());
            filteredBlock.Count.ShouldBe(1);

            filteredBlock = await transactionFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filter);
            filteredBlock.Count.ShouldBe(1);
            filteredBlock[0].Transactions.Count().ShouldBe(2);
            filteredBlock[0].Transactions[0].LogEvents.Count().ShouldBe(3);
            filteredBlock[0].Transactions[1].LogEvents.Count().ShouldBe(2);
            
            filteredBlock = await transactionFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filterOnlyContractAddress);
            filteredBlock.Count.ShouldBe(1);
            filteredBlock[0].Transactions.Count().ShouldBe(1);
            filteredBlock[0].Transactions[0].LogEvents.Count().ShouldBe(3);

            filteredBlock =
                await transactionFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filterNotExist);
            filteredBlock.Count.ShouldBe(0);
        }

        {
            var logEventFilterProvider = _blockFilterProviders.First(o => o.FilterType == BlockFilterType.LogEvent);

            var filteredBlock = await logEventFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, null);
            filteredBlock.Count.ShouldBe(1);

            filteredBlock = await logEventFilterProvider.FilterBlocksAsync(new List<BlockDto> { block },
                new List<FilterContractEventInput>());
            filteredBlock.Count.ShouldBe(1);

            filteredBlock = await logEventFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filter);
            filteredBlock.Count.ShouldBe(1);
            filteredBlock[0].Transactions.Count().ShouldBe(2);
            filteredBlock[0].Transactions[0].LogEvents.Count().ShouldBe(2);
            filteredBlock[0].Transactions[1].LogEvents.Count().ShouldBe(2);
            
            filteredBlock = await logEventFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filterOnlyContractAddress);
            filteredBlock.Count.ShouldBe(1);
            filteredBlock[0].Transactions.Count().ShouldBe(1);
            filteredBlock[0].Transactions[0].LogEvents.Count().ShouldBe(1);

            filteredBlock =
                await logEventFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filterNotExist);
            filteredBlock.Count.ShouldBe(0);
        }
    }

    private BlockDto MockBlock()
    {
        var chainId = "AELF";
        var blockNum = 1000;
        
        var blockHash = "BlockHash";
        return  new BlockDto
        {
            ChainId = chainId,
            BlockHash = blockHash,
            BlockNumber = blockNum,
            IsConfirmed = true,
            BlockTime = DateTime.UtcNow,
            PreviousBlockHash = "PreviousHash",
            Transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    ChainId = chainId,
                    TransactionId = Guid.NewGuid().ToString(),
                    BlockHash = blockHash,
                    BlockNumber = blockNum,
                    IsConfirmed = true,
                    BlockTime = DateTime.UtcNow,
                    PreviousBlockHash = "PreviousHash",
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = Guid.NewGuid().ToString(),
                            BlockHash = blockHash,
                            BlockNumber = blockNum,
                            IsConfirmed = true,
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
                            BlockNumber = blockNum,
                            IsConfirmed = true,
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
                            BlockNumber = blockNum,
                            IsConfirmed = true,
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
                    BlockNumber = blockNum,
                    IsConfirmed = true,
                    BlockTime = DateTime.UtcNow,
                    PreviousBlockHash = "PreviousHash",
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = Guid.NewGuid().ToString(),
                            BlockHash = blockHash,
                            BlockNumber = blockNum,
                            IsConfirmed = true,
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
                            BlockNumber = blockNum,
                            IsConfirmed = true,
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