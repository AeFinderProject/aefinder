using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Block.Dtos;

namespace AElfIndexer.App.Handlers;

public static class MockHandlerHelper
{
    public static List<BlockWithTransactionDto> CreateBlock(long startBlock, long blockCount, string blockHash,
        string chainId, string perHash = "", bool confirmed = false)
    {
        var blocks = new List<BlockWithTransactionDto>();
        for (var i = startBlock; i < startBlock + blockCount; i++)
        {
            var perBlockHash = i == startBlock && perHash != "" ? perHash : blockHash + (i - 1);
            var blockWithTransactionDto = new BlockWithTransactionDto
            {
                Id = i.ToString(),
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

    public static List<BlockWithTransactionDto> CreateBlockWithTransactionDtos(
        long startBlock, long blockCount, string blockHash, string chainId,
        long transactionCount, string transactionId, TransactionStatus status,
        string perHash = "", bool confirmed = false)
    {
        var blocks = new List<BlockWithTransactionDto>();
        for (var i = startBlock; i < startBlock + blockCount; i++)
        {
            var perBlockHash = i == startBlock && perHash != "" ? perHash : blockHash + (i - 1);
            var hash = blockHash + i;
            var dateTime = DateTime.UtcNow.AddMilliseconds(i);
            var transactions = CreateTransaction(
                transactionCount, transactionId,
                hash, i, dateTime,
                chainId, perBlockHash, confirmed, status);
            var transactionIds = transactions.Select(tx => tx.TransactionId).ToList();

            var blockWithTransactionDto = new BlockWithTransactionDto
            {
                Id = i.ToString(),
                Confirmed = confirmed,
                BlockHash = hash,
                BlockHeight = i,
                BlockTime = dateTime,
                ChainId = chainId,
                PreviousBlockHash = perBlockHash,
                TransactionIds = transactionIds,
                Transactions = transactions
            };
            blocks.Add(blockWithTransactionDto);
        }

        return blocks;
    }

    public static List<BlockWithTransactionDto> CreateBlockWithTransactionDtosAndTransferredLogEvent(
        long startBlock, long blockCount, string blockHash, string chainId,
        long transactionCount, string transactionId, TransactionStatus status,
        long logEventCount, string perHash = "", bool confirmed = false)
    {
        var blocks = new List<BlockWithTransactionDto>();
        for (var i = startBlock; i < startBlock + blockCount; i++)
        {
            var perBlockHash = i == startBlock && perHash != "" ? perHash : blockHash + (i - 1);
            var hash = blockHash + i;
            var dateTime = DateTime.UtcNow.AddMilliseconds(i);
            var transactions = CreateTransaction(
                transactionCount, transactionId,
                hash, i, dateTime,
                chainId, perBlockHash, confirmed, status,
                logEventCount);
            var transactionIds = transactions.Select(tx => tx.TransactionId).ToList();

            var blockWithTransactionDto = new BlockWithTransactionDto
            {
                Id = i.ToString(),
                Confirmed = confirmed,
                BlockHash = hash,
                BlockHeight = i,
                BlockTime = dateTime,
                ChainId = chainId,
                PreviousBlockHash = perBlockHash,
                TransactionIds = transactionIds,
                Transactions = transactions
            };
            blocks.Add(blockWithTransactionDto);
        }

        return blocks;
    }

    private static List<TransactionDto> CreateTransaction(
        long transactionCount, string transactionId,
        string blockHash, long blockHeight, DateTime blockTime,
        string chainId, string perHash, bool confirmed, TransactionStatus status,
        long logEventCount = 0)
    {
        var transactions = new List<TransactionDto>();
        for (var i = 0; i < transactionCount; i++)
        {
            var id = blockHeight+transactionId + i;
            var logEvents = logEventCount > 0
                ? CreateTransferLogEvent(logEventCount, id,
                    blockHash, blockHeight, blockTime, chainId, perHash, confirmed)
                : new List<LogEventDto>();
            var transactionDto = new TransactionDto
            {
                TransactionId = id,
                Confirmed = confirmed,
                PreviousBlockHash = perHash,
                BlockHash = blockHash,
                BlockHeight = blockHeight,
                BlockTime = blockTime,
                ChainId = chainId,
                Status = status,
                LogEvents = logEvents
            };
            transactions.Add(transactionDto);
        }

        return transactions;
    }

    private static List<LogEventDto> CreateTransferLogEvent(
        long logEventCount, string transactionId,
        string blockHash, long blockHeight, DateTime blockTime,
        string chainId, string perHash, bool confirmed)
    {
        var logEventDtos = new List<LogEventDto>();
        for (var i = 0; i < logEventCount; i++)
        {
            var transferred = new Transferred
            {
                Amount = blockHeight.Add(i),
                From = Address.FromBase58("2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e"),
                To = Address.FromBase58("2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e"),
                Symbol = "TEST"
            };
            var transferredToLogEvent = transferred.ToLogEvent();
            var logEventDto = new LogEventDto
            {
                TransactionId = transactionId,
                Confirmed = confirmed,
                PreviousBlockHash = perHash,
                BlockHash = blockHash,
                BlockHeight = blockHeight,
                BlockTime = blockTime,
                ChainId = chainId,
                Index = i,
                ContractAddress = "TokenContractAddress",
                EventName = "Transferred",
                ExtraProperties = new Dictionary<string, string>
                {
                    ["Indexed"] = transferredToLogEvent.Indexed.ToString(),
                    ["NonIndexed"] = transferredToLogEvent.NonIndexed.ToBase64()
                }
            };
            logEventDtos.Add(logEventDto);
        }

        return logEventDtos;
    }
}