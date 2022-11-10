using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfScan.AElf.Dtos;
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
    public async Task FilterBlocks_RepeatedEvent_Test()
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
        
        var filterNotExist = new List<FilterContractEventInput>
        {
            new FilterContractEventInput
            {
                ContractAddress = "ContractAddress2",
                EventNames = new List<string> { "EventName" }
            }
        };

        var blockFilterProvider = _blockFilterProviders.First(o => o.FilterType == BlockFilterType.Block);
        var filteredBlock = await blockFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filter);
        filteredBlock.Count.ShouldBe(1);
        filteredBlock = await blockFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filterNotExist);
        filteredBlock.Count.ShouldBe(0);
        
        var transactionFilterProvider = _blockFilterProviders.First(o => o.FilterType == BlockFilterType.Transaction);
        filteredBlock = await transactionFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filter);
        filteredBlock.Count.ShouldBe(1);
        filteredBlock = await transactionFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filterNotExist);
        filteredBlock.Count.ShouldBe(0);
        
        var logEventFilterProvider = _blockFilterProviders.First(o => o.FilterType == BlockFilterType.LogEvent);
        filteredBlock = await logEventFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filter);
        filteredBlock.Count.ShouldBe(1);
        filteredBlock = await logEventFilterProvider.FilterBlocksAsync(new List<BlockDto> { block }, filterNotExist);
        filteredBlock.Count.ShouldBe(0);
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