using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block;
using AElfIndexer.Block.Dtos;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Grains.BlockScan;

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
        var filter = GetFilter(input.Events);
        for (var i = input.StartBlockHeight; i <= input.EndBlockHeight; i++)
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
        for (var i = input.StartBlockHeight; i <= input.EndBlockHeight; i++)
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