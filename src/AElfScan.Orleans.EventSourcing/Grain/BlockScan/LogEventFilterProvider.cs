using AElfScan.AElf;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Entities.Es;

namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public class LogEventFilterProvider : IBlockFilterProvider
{
    private readonly IBlockAppService _blockAppService;

    public BlockFilterType FilterType { get; } = BlockFilterType.LogEvent;

    public LogEventFilterProvider(IBlockAppService blockAppService)
    {
        _blockAppService = blockAppService;
    }
    
    public async Task<List<BlockDto>> GetBlocksAsync(string chainId, long startBlockNumber, long endBlockNumber,
        bool onlyConfirmed, List<FilterContractEventInput> filters)
    {
        var logEvents = await _blockAppService.GetLogEventsAsync(new GetLogEventsInput()
        {
            ChainId = chainId,
            StartBlockNumber = startBlockNumber,
            EndBlockNumber = endBlockNumber,
            //IsOnlyConfirmed = onlyConfirmed, // TODO: need add this parameter
            Contracts = filters
        });

        var transactions = new Dictionary<string, TransactionDto>();
        foreach (var logEvent in logEvents)
        {
            if (transactions.TryGetValue(logEvent.TransactionId, out var transaction))
            {
                transaction.LogEvents.Add(logEvent);
            }
            else
            {
                transaction = new TransactionDto
                {
                    ChainId = logEvent.ChainId,
                    BlockHash = logEvent.BlockHash,
                    BlockNumber = logEvent.BlockNumber,
                    BlockTime = logEvent.BlockTime,
                    PreviousBlockHash = logEvent.PreviousBlockHash,
                    TransactionId = logEvent.TransactionId,
                    LogEvents = new List<LogEventDto> { logEvent }
                };
                transactions.Add(logEvent.TransactionId, transaction);
            }
        }
        
        var blocks = new Dictionary<string, BlockDto>();
        foreach (var transaction in transactions.Values)
        {
            if (blocks.TryGetValue(transaction.BlockHash, out var block))
            {
                block.Transactions.Add(transaction);
            }
            else
            {
                block = new BlockDto
                {
                    ChainId = transaction.ChainId,
                    BlockHash = transaction.BlockHash,
                    BlockNumber = transaction.BlockNumber,
                    BlockTime = transaction.BlockTime,
                    IsConfirmed = transaction.IsConfirmed,
                    PreviousBlockHash = transaction.PreviousBlockHash,
                    Transactions = new List<TransactionDto>
                    {
                        transaction
                    }
                };
                blocks.Add(block.BlockHash, block);
            }
        }

        return blocks.Values.ToList();
    }

    public async Task<List<BlockDto>> FilterBlocksAsync(List<BlockDto> blocks, List<FilterContractEventInput> filters)
    {
        if (filters == null || filters.Count == 0)
        {
            return blocks;
        }

        var contractAddressFilter = new HashSet<string>();
        var logEventFilter = new HashSet<string>();
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

        var result = new List<BlockDto>();
        foreach (var block in blocks)
        {
            var filteredBlock = new BlockDto
            {
                ChainId = block.ChainId,
                BlockHash = block.BlockHash,
                BlockNumber = block.BlockNumber,
                BlockTime = block.BlockTime,
                PreviousBlockHash = block.PreviousBlockHash,
                Transactions = new List<TransactionDto>()
            };

            foreach (var transaction in block.Transactions)
            {
                var filteredTransaction = new TransactionDto
                {
                    ChainId = transaction.ChainId,
                    BlockHash = transaction.BlockHash,
                    BlockNumber = transaction.BlockNumber,
                    BlockTime = transaction.BlockTime,
                    PreviousBlockHash = transaction.PreviousBlockHash,
                    TransactionId = transaction.TransactionId,
                    LogEvents = new List<LogEventDto>()
                };

                foreach (var logEvent in transaction.LogEvents.Where(logEvent =>
                             (contractAddressFilter.Count > 0 &&
                              contractAddressFilter.Contains(logEvent.ContractAddress)) ||
                             (logEventFilter.Count > 0 &&
                              logEventFilter.Contains(logEvent.ContractAddress + logEvent.EventName))))
                {
                    filteredTransaction.LogEvents.Add(logEvent);
                }

                if (filteredTransaction.LogEvents.Count > 0)
                {
                    filteredBlock.Transactions.Add(filteredTransaction);
                }
            }

            if (filteredBlock.Transactions.Count > 0)
            {
                result.Add(filteredBlock);
            }
        }

        return result;
    }
}