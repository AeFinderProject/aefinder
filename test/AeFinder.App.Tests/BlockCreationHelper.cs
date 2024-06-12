using System;
using System.Collections.Generic;
using System.Linq;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Types;

namespace AeFinder.App;

public static class BlockCreationHelper
{
    public static List<AppSubscribedBlockDto> CreateBlock(long startBlock, long blockCount, string blockHash,
        string chainId, string perHash = "", bool confirmed = false)
    {
        var blocks = new List<AppSubscribedBlockDto>();
        for (var i = startBlock; i < startBlock + blockCount; i++)
        {
            var perBlockHash = i == startBlock && perHash != "" ? perHash : blockHash + (i - 1);
            var blockWithTransactionDto = new AppSubscribedBlockDto
            {
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

    public static List<AppSubscribedBlockDto> CreateBlockWithTransactions(
        long startBlock, long blockCount, string blockHash, string chainId,
        long transactionCount, string transactionId, TransactionStatus status,
        string perHash = "", bool confirmed = false)
    {
        var blocks = new List<AppSubscribedBlockDto>();
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

            var blockWithTransactionDto = new AppSubscribedBlockDto
            {
                Confirmed = confirmed,
                BlockHash = hash,
                BlockHeight = i,
                BlockTime = dateTime,
                ChainId = chainId,
                PreviousBlockHash = perBlockHash,
                Transactions = transactions
            };
            blocks.Add(blockWithTransactionDto);
        }

        return blocks;
    }

    public static List<AppSubscribedBlockDto> CreateBlockWithTransactionAndTransferredLogEvents(
        long startBlock, long blockCount, string blockHash, string chainId,
        long transactionCount, string transactionId, TransactionStatus status,
        long logEventCount, string perHash = "", bool confirmed = false)
    {
        var blocks = new List<AppSubscribedBlockDto>();
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

            var blockWithTransactionDto = new AppSubscribedBlockDto
            {
                Confirmed = confirmed,
                BlockHash = hash,
                BlockHeight = i,
                BlockTime = dateTime,
                ChainId = chainId,
                PreviousBlockHash = perBlockHash,
                Transactions = transactions
            };
            blocks.Add(blockWithTransactionDto);
        }

        return blocks;
    }

    private static List<AppSubscribedTransactionDto> CreateTransaction(
        long transactionCount, string transactionId,
        string blockHash, long blockHeight, DateTime blockTime,
        string chainId, string perHash, bool confirmed, TransactionStatus status,
        long logEventCount = 0)
    {
        var transactions = new List<AppSubscribedTransactionDto>();
        for (var i = 0; i < transactionCount; i++)
        {
            var id = blockHash + transactionId + i;
            var logEvents = logEventCount > 0
                ? CreateTransferLogEvent(logEventCount, id,
                    blockHash, blockHeight, blockTime, chainId, perHash, confirmed)
                : new List<AppSubscribedLogEventDto>();
            var transactionDto = new AppSubscribedTransactionDto
            {
                TransactionId = id,
                Status = status,
                LogEvents = logEvents
            };
            transactions.Add(transactionDto);
        }

        return transactions;
    }

    private static List<AppSubscribedLogEventDto> CreateTransferLogEvent(
        long logEventCount, string transactionId,
        string blockHash, long blockHeight, DateTime blockTime,
        string chainId, string perHash, bool confirmed)
    {
        var logEventDtos = new List<AppSubscribedLogEventDto>();
        for (var i = 0; i < logEventCount; i++)
        {
            var transferred = new Transferred
            {
                Amount = blockHeight.Add(i),
                From = Address.FromBase58("2pL7foxBhMC1RVZMUEtkvYK4pWWaiLHBAQcXFdzfD5oZjYSr3e"),
                To = Address.FromBase58("xZ4UWtQEUzGgmjByxf6248sJuqgiXWVK36EGtzyp9Xtp4B2h4"),
                Symbol = "TEST"
            };
            var transferredToLogEvent = transferred.ToLogEvent();
            var logEventDto = new AppSubscribedLogEventDto
            {
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