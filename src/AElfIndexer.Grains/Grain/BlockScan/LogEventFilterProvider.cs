using AElfIndexer.Block;
using AElfIndexer.Block.Dtos;

namespace AElfIndexer.Grains.Grain.BlockScan;

public class LogEventFilterProvider : IBlockFilterProvider
{
    private readonly IBlockAppService _blockAppService;

    public BlockFilterType FilterType { get; } = BlockFilterType.LogEvent;

    public LogEventFilterProvider(IBlockAppService blockAppService)
    {
        _blockAppService = blockAppService;
    }

    public async Task<List<BlockWithTransactionDto>> GetBlocksAsync(string chainId, long startBlockNumber, long endBlockNumber,
        bool onlyConfirmed, List<FilterContractEventInput> filters)
    {
        var logEvents = await _blockAppService.GetLogEventsAsync(new GetLogEventsInput()
        {
            ChainId = chainId,
            StartBlockHeight = startBlockNumber,
            EndBlockHeight = endBlockNumber,
            IsOnlyConfirmed = onlyConfirmed,
            Events = filters
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
                    BlockHeight = logEvent.BlockHeight,
                    BlockTime = logEvent.BlockTime,
                    PreviousBlockHash = logEvent.PreviousBlockHash,
                    IsConfirmed = logEvent.IsConfirmed,
                    TransactionId = logEvent.TransactionId,
                    LogEvents = new List<LogEventDto> { logEvent }
                };
                transactions.Add(logEvent.TransactionId, transaction);
            }
        }

        var blocks = new Dictionary<string, BlockWithTransactionDto>();
        foreach (var transaction in transactions.Values)
        {
            if (blocks.TryGetValue(transaction.BlockHash, out var block))
            {
                block.Transactions.Add(transaction);
            }
            else
            {
                block = new BlockWithTransactionDto
                {
                    ChainId = transaction.ChainId,
                    BlockHash = transaction.BlockHash,
                    BlockHeight = transaction.BlockHeight,
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

    public async Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks, List<FilterContractEventInput> filters)
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

        var result = new List<BlockWithTransactionDto>();
        foreach (var block in blocks)
        {
            var filteredBlock = new BlockWithTransactionDto
            {
                ChainId = block.ChainId,
                BlockHash = block.BlockHash,
                BlockHeight = block.BlockHeight,
                BlockTime = block.BlockTime,
                PreviousBlockHash = block.PreviousBlockHash,
                IsConfirmed = block.IsConfirmed,
                Transactions = new List<TransactionDto>()
            };

            foreach (var transaction in block.Transactions)
            {
                var filteredTransaction = new TransactionDto
                {
                    ChainId = transaction.ChainId,
                    BlockHash = transaction.BlockHash,
                    BlockHeight = transaction.BlockHeight,
                    BlockTime = transaction.BlockTime,
                    PreviousBlockHash = transaction.PreviousBlockHash,
                    IsConfirmed = transaction.IsConfirmed,
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

            result.Add(filteredBlock);
        }

        return result;
    }
}