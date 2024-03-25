using System;
using System.Collections.Generic;
using System.Linq;
using AeFinder.Block.Dtos;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Grains.BlockScan;

public interface IBlockDataProvider
{
    Dictionary<long, List<BlockWithTransactionDto>> Blocks { get; }
}

public class BlockDataProvider:IBlockDataProvider,ISingletonDependency
{
    public Dictionary<long, List<BlockWithTransactionDto>> Blocks { get; }

    public BlockDataProvider()
    {
        Blocks = new Dictionary<long, List<BlockWithTransactionDto>>();
        var chainId = "AELF";
        var previousHash = "PreviousHash";
        for (var i = 1; i <= 50; i++)
        {
            var block = MockBlock(chainId, i, previousHash, true, "0");
            Blocks[i]= new List<BlockWithTransactionDto>{block};
            previousHash = block.BlockHash;
        }
        
        for (var i = 51; i <= 60; i++)
        {
            var block = MockBlock(chainId, i, previousHash, false, "1");
            Blocks[i]= new List<BlockWithTransactionDto>{block};
            previousHash = block.BlockHash;
        }

        previousHash = Blocks[50].First().BlockHash;
        for (var i = 51; i <= 55; i++)
        {
            var block = MockBlock(chainId, i, previousHash, false, "2");
            Blocks[i].Add(block);
            previousHash = block.BlockHash;
        }
    }

    private BlockWithTransactionDto MockBlock(string chainId, long blockNum, string previousHash, bool confirmed, string branch)
    {
        var blockHash = $"BlockHash-{blockNum}-{branch}";
        var txId1 = Guid.NewGuid().ToString();
        var txId2 = Guid.NewGuid().ToString();
        return new BlockWithTransactionDto
        {
            ChainId = chainId,
            BlockHash = blockHash,
            BlockHeight = blockNum,
            Confirmed = confirmed,
            BlockTime = DateTime.UtcNow,
            PreviousBlockHash = previousHash,
            TransactionIds = new List<string>{txId1,txId2},
            LogEventCount = 3,
            Transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    ChainId = chainId,
                    TransactionId = txId1,
                    BlockHash = blockHash,
                    BlockHeight = blockNum,
                    Confirmed = confirmed,
                    BlockTime = DateTime.UtcNow,
                    PreviousBlockHash = previousHash,
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = txId1,
                            BlockHash = blockHash,
                            BlockHeight = blockNum,
                            Confirmed = confirmed,
                            BlockTime = DateTime.UtcNow,
                            PreviousBlockHash = previousHash,
                            ContractAddress = "ContractAddress" + blockNum % 10,
                            EventName = "EventName" + blockNum
                        }
                    }
                },
                new TransactionDto
                {
                    ChainId = chainId,
                    TransactionId = txId2,
                    BlockHash = blockHash,
                    BlockHeight = blockNum,
                    Confirmed = confirmed,
                    BlockTime = DateTime.UtcNow,
                    PreviousBlockHash = previousHash,
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = txId2,
                            BlockHash = blockHash,
                            BlockHeight = blockNum,
                            Confirmed = confirmed,
                            BlockTime = DateTime.UtcNow,
                            PreviousBlockHash = previousHash,
                            ContractAddress = "FakeContractAddress1" + blockNum % 10,
                            EventName = "FakeEventName1" + blockNum
                        },
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = txId2,
                            BlockHash = blockHash,
                            BlockHeight = blockNum,
                            Confirmed = confirmed,
                            BlockTime = DateTime.UtcNow,
                            PreviousBlockHash = previousHash,
                            ContractAddress = "FakeContractAddress2" + blockNum % 10,
                            EventName = "FakeEventName2" + blockNum
                        }
                    }
                }
            }
        };
    }
}