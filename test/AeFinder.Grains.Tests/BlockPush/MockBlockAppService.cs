using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Block;
using AeFinder.Block.Dtos;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Grains.BlockPush;

public class MockBlockAppService : IBlockAppService, ISingletonDependency
{
    private readonly IBlockDataProvider _blockDataProvider;

    public MockBlockAppService(IBlockDataProvider blockDataProvider)
    {
        _blockDataProvider = blockDataProvider;
    }

    public async Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input)
    {
        var result = new List<BlockDto>();
        for (var i = input.StartBlockHeight; i <= input.EndBlockHeight; i++)
        {
            if (!_blockDataProvider.Blocks.TryGetValue(i, out var block))
            {
                continue;
            }

            result.AddRange(_blockDataProvider.Blocks[i].Where(o => !input.IsOnlyConfirmed || o.Confirmed).Select(
                block => new BlockDto
                {
                    Id = block.Id,
                    Signature = block.Signature,
                    BlockHash = block.BlockHash,
                    BlockHeight = block.BlockHeight,
                    BlockTime = block.BlockTime,
                    ChainId = block.ChainId,
                    ExtraProperties = block.ExtraProperties,
                    Confirmed = block.Confirmed,
                    SignerPubkey = block.SignerPubkey,
                    PreviousBlockHash = block.PreviousBlockHash,
                    TransactionIds = block.Transactions.Select(o => o.TransactionId).ToList(),
                    LogEventCount = block.Transactions.Sum(o => o.LogEvents.Count)
                }));
        }

        return result;
    }

    public async Task<long> GetBlockCountAsync(GetBlocksInput input)
    {
        return input.EndBlockHeight - input.StartBlockHeight + 1;
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        var result = new List<TransactionDto>();
        var filter = GetContractEventFilter(input.Events);
        for (var i = input.StartBlockHeight; i <= input.EndBlockHeight; i++)
        {
            if (_blockDataProvider.Blocks.TryGetValue(i, out var blocks))
            {
                foreach (var block in blocks)
                {
                    foreach (var transaction in block.Transactions.Where(transaction => transaction.LogEvents.Any(
                                 logEvent =>
                                     (filter.Item1.Count == 0 && filter.Item2.Count == 0 ||
                                      filter.Item1.Count != 0 && filter.Item1.Contains(logEvent.ContractAddress) ||
                                      filter.Item2.Count != 0 &&
                                      filter.Item2.Contains(logEvent.ContractAddress + logEvent.EventName)))))
                    {
                        result.Add(transaction);
                    }
                }
            }
        }

        return result;
    }

    public async Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input)
    {
        var result = new List<LogEventDto>();
        var filter = GetContractEventFilter(input.Events);
        for (var i = input.StartBlockHeight; i <= input.EndBlockHeight; i++)
        {
            if (_blockDataProvider.Blocks.TryGetValue(i, out var blocks))
            {
                foreach (var transaction in blocks.SelectMany(block => block.Transactions))
                {
                    result.AddRange((from logEvent in transaction.LogEvents
                        where filter.Item1.Count == 0 && filter.Item2.Count == 0 ||
                              filter.Item1.Count != 0 && filter.Item1.Contains(logEvent.ContractAddress) ||
                              filter.Item2.Count != 0 &&
                              filter.Item2.Contains(logEvent.ContractAddress + logEvent.EventName)
                        select logEvent).ToList());
                }
            }
        }

        return result;
    }

    public async Task<List<TransactionDto>> GetSubscriptionTransactionsAsync(GetSubscriptionTransactionsInput input)
    {
        var result = new List<TransactionDto>();
        var transactionFilter = GetTransactionFilter(input.TransactionFilters);
        var contractEventFilter = GetContractEventFilter(input.LogEventFilters);
        for (var i = input.StartBlockHeight; i <= input.EndBlockHeight; i++)
        {
            if (_blockDataProvider.Blocks.TryGetValue(i, out var blocks))
            {
                foreach (var block in blocks)
                {
                    if (input.IsOnlyConfirmed && !block.Confirmed)
                    {
                        continue;
                    }

                    foreach (var transaction in block.Transactions)
                    {
                        if ((transactionFilter.Item1.Count == 0 && transactionFilter.Item2.Count == 0 &&
                             contractEventFilter.Item1.Count == 0 && contractEventFilter.Item2.Count == 0) ||
                            (transactionFilter.Item1.Count != 0 && transactionFilter.Item1.Contains(transaction.To) ||
                             transactionFilter.Item2.Count != 0 &&
                             transactionFilter.Item2.Contains(transaction.To + transaction.MethodName) ||
                             transaction.LogEvents.Any(
                                 logEvent =>
                                     contractEventFilter.Item1.Count != 0 &&
                                     contractEventFilter.Item1.Contains(logEvent.ContractAddress) ||
                                     contractEventFilter.Item2.Count != 0 &&
                                     contractEventFilter.Item2.Contains(logEvent.ContractAddress +
                                                                        logEvent.EventName))))
                        {
                            result.Add(transaction);
                        }
                    }
                }
            }
        }

        return result;
    }
    
    private Tuple<HashSet<string>, HashSet<string>> GetTransactionFilter(List<FilterTransactionInput> filters)
    {
        var toFilter = new HashSet<string>();
        var methodNameFilter = new HashSet<string>();
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (filter.MethodNames == null || filter.MethodNames.Count == 0)
                {
                    toFilter.Add(filter.To);
                }
                else
                {
                    foreach (var methodName in filter.MethodNames)
                    {
                        methodNameFilter.Add(filter.To + methodName);
                    }
                }
            }
        }

        return new Tuple<HashSet<string>, HashSet<string>>(toFilter, methodNameFilter);
    }

    private Tuple<HashSet<string>, HashSet<string>> GetContractEventFilter(List<FilterContractEventInput> filters)
    {
        var contractAddressFilter = new HashSet<string>();
        var logEventFilter = new HashSet<string>();
        if (filters != null)
        {
            foreach (var filter in filters)
            {
                if (filter.EventNames == null || filter.EventNames.Count == 0)
                {
                    contractAddressFilter.Add(filter.ContractAddress);
                }
                else
                {
                    foreach (var eventName in filter.EventNames)
                    {
                        logEventFilter.Add(filter.ContractAddress + eventName);
                    }
                }
            }
        }

        return new Tuple<HashSet<string>, HashSet<string>>(contractAddressFilter, logEventFilter);
    }
}