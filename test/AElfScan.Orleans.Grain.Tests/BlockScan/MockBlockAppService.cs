using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using AElfScan.AElf;
using AElfScan.AElf.Dtos;
using Volo.Abp.DependencyInjection;

namespace AElfScan.BlockScan;

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
        var filter = GetFilter(input.Events);
        for (var i = input.StartBlockNumber; i <= input.EndBlockNumber; i++)
        {
            result.AddRange((from block in _blockDataProvider.Blocks[i]
                from transaction in block.Transactions
                from logEvent in transaction.LogEvents
                where filter.Item1.Count ==0 && filter.Item2.Count ==0 ||
                      filter.Item1.Count !=0 && filter.Item1.Contains(logEvent.ContractAddress) ||
                      filter.Item2.Count !=0 && filter.Item2.Contains(logEvent.ContractAddress + logEvent.EventName)
                select block).ToList());
        }

        return result;
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        var result = new List<TransactionDto>();
        var filter = GetFilter(input.Events);
        for (var i = input.StartBlockNumber; i <= input.EndBlockNumber; i++)
        {
            foreach (var block in _blockDataProvider.Blocks[i])
            {
                result.AddRange((from transaction in block.Transactions
                    from logEvent in transaction.LogEvents
                    where filter.Item1.Count ==0 && filter.Item2.Count ==0 ||
                          filter.Item1.Count !=0 && filter.Item1.Contains(logEvent.ContractAddress) ||
                          filter.Item2.Count !=0 && filter.Item2.Contains(logEvent.ContractAddress + logEvent.EventName)
                    select transaction).ToList());
            }
        }

        return result;
    }

    public async Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input)
    {
        var result = new List<LogEventDto>();
        var filter = GetFilter(input.Events);
        for (var i = input.StartBlockNumber; i <= input.EndBlockNumber; i++)
        {
            foreach (var transaction in _blockDataProvider.Blocks[i].SelectMany(block => block.Transactions))
            {
                result.AddRange((from logEvent in transaction.LogEvents
                    where filter.Item1.Count ==0 && filter.Item2.Count ==0 ||
                          filter.Item1.Count !=0 && filter.Item1.Contains(logEvent.ContractAddress) ||
                          filter.Item2.Count !=0 && filter.Item2.Contains(logEvent.ContractAddress + logEvent.EventName)
                    select logEvent).ToList());
            }
        }

        return result;
    }

    private Tuple<HashSet<string>, HashSet<string>> GetFilter(List<FilterContractEventInput> filters)
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