using System;
using System.Collections.Generic;
using System.Linq;
using AElfScan.Block.Dtos;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Grains.BlockScan;

public interface IBlockDataProvider
{
    Dictionary<long, List<BlockDto>> Blocks { get; }
}

public class BlockDataProvider:IBlockDataProvider,ISingletonDependency
{
    public Dictionary<long, List<BlockDto>> Blocks { get; }

    public BlockDataProvider()
    {
        Blocks = new Dictionary<long, List<BlockDto>>();
        var chainId = "AELF";
        var previousHash = "PreviousHash";
        for (var i = 1; i <= 50; i++)
        {
            var block = MockBlock(chainId, i, previousHash, true, "0");
            Blocks[i]= new List<BlockDto>{block};
            previousHash = block.BlockHash;
        }
        
        for (var i = 51; i <= 60; i++)
        {
            var block = MockBlock(chainId, i, previousHash, false, "1");
            Blocks[i]= new List<BlockDto>{block};
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

    private BlockDto MockBlock(string chainId, long blockNum, string previousHash, bool isConfirmed, string branch)
    {
        var blockHash = $"BlockHash-{blockNum}-{branch}";
        return new BlockDto
        {
            ChainId = chainId,
            BlockHash = blockHash,
            BlockNumber = blockNum,
            IsConfirmed = isConfirmed,
            BlockTime = DateTime.UtcNow,
            PreviousBlockHash = previousHash,
            Transactions = new List<TransactionDto>
            {
                new TransactionDto
                {
                    ChainId = chainId,
                    TransactionId = Guid.NewGuid().ToString(),
                    BlockHash = blockHash,
                    BlockNumber = blockNum,
                    IsConfirmed = isConfirmed,
                    BlockTime = DateTime.UtcNow,
                    PreviousBlockHash = previousHash,
                    LogEvents = new List<LogEventDto>
                    {
                        new LogEventDto
                        {
                            ChainId = chainId,
                            TransactionId = Guid.NewGuid().ToString(),
                            BlockHash = blockHash,
                            BlockNumber = blockNum,
                            IsConfirmed = isConfirmed,
                            BlockTime = DateTime.UtcNow,
                            PreviousBlockHash = previousHash,
                            ContractAddress = "ContractAddress" + blockNum % 10,
                            EventName = "EventName" + blockNum
                        }
                    }
                }
            }
        };
    }
}