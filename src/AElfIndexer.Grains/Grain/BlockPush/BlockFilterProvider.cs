using AElfIndexer.Block;
using AElfIndexer.Block.Dtos;
using AElfIndexer.Grains.Grain.Subscriptions;
using AElfIndexer.Grains.State.Subscriptions;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Grains.Grain.BlockPush;

public class BlockFilterProvider : IBlockFilterProvider, ITransientDependency
{
    private readonly IBlockAppService _blockAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<BlockFilterProvider> _logger;

    public BlockFilterProvider(IBlockAppService blockAppService, ILogger<BlockFilterProvider> logger,
        IObjectMapper objectMapper)
    {
        _blockAppService = blockAppService;
        _logger = logger;
        _objectMapper = objectMapper;
    }

    public async Task<List<BlockWithTransactionDto>> GetBlocksAsync(GetSubscriptionTransactionsInput input)
    {
        var blocks = await _blockAppService.GetBlocksAsync(new GetBlocksInput
        {
            ChainId = input.ChainId,
            StartBlockHeight = input.StartBlockHeight,
            EndBlockHeight = input.EndBlockHeight,
            IsOnlyConfirmed = input.IsOnlyConfirmed
        });

        if (blocks.Count == 0)
        {
            return new List<BlockWithTransactionDto>();
        }

        if (blocks.First().BlockHeight != input.StartBlockHeight)
        {
            throw new ApplicationException(
                $"Get Transaction filed, ChainId {input.ChainId} StartBlockHeight {input.StartBlockHeight} EndBlockHeight {input.EndBlockHeight} OnlyConfirmed {input.IsOnlyConfirmed}, Result first block height {blocks.First().BlockHeight}");
        }

        var blockDic =
            blocks.ToDictionary(o => o.BlockHash, o => _objectMapper.Map<BlockDto, BlockWithTransactionDto>(o));

        var transactions = await _blockAppService.GetSubscriptionTransactionsAsync(input);
        foreach (var transaction in transactions)
        {
            if (blockDic.TryGetValue(transaction.BlockHash, out var block))
            {
                block.Transactions.Add(transaction);
            }
        }

        return blockDic.Values.ToList();
    }

    public async Task<List<BlockWithTransactionDto>> FilterBlocksAsync(List<BlockWithTransactionDto> blocks,
        List<TransactionCondition> transactionConditions, List<LogEventCondition> logEventConditions)
    {
        if (transactionConditions.IsNullOrEmpty() && logEventConditions.IsNullOrEmpty())
        {
            return blocks;
        }

        var transactionToFilter = new HashSet<string>();
        var transactionMethodFilter = new HashSet<string>();
        if (transactionConditions != null)
        {
            foreach (var condition in transactionConditions)
            {
                if (condition.MethodNames.Count == 0)
                {
                    transactionToFilter.Add(condition.To);
                }
                else
                {
                    foreach (var methodName in condition.MethodNames)
                    {
                        transactionMethodFilter.Add(condition.To + methodName);
                    }
                }
            }
        }

        var contractAddressFilter = new HashSet<string>();
        var logEventFilter = new HashSet<string>();
        if (logEventConditions != null)
        {
            foreach (var condition in logEventConditions)
            {
                if (condition.EventNames.Count == 0)
                {
                    contractAddressFilter.Add(condition.ContractAddress);
                }
                else
                {
                    foreach (var eventName in condition.EventNames)
                    {
                        logEventFilter.Add(condition.ContractAddress + eventName);
                    }
                }
            }
        }

        foreach (var block in blocks)
        {
            var filteredTransactions = new List<TransactionDto>();
            foreach (var transaction in block.Transactions)
            {
                if (transactionToFilter.Contains(transaction.To) ||
                    transactionMethodFilter.Contains(transaction.To + transaction.MethodName))
                {
                    filteredTransactions.Add(transaction);
                    continue;
                }

                if (contractAddressFilter.Count > 0 && transaction.LogEvents.Any(logEvent =>
                        contractAddressFilter.Contains(logEvent.ContractAddress)))
                {
                    filteredTransactions.Add(transaction);
                    continue;
                }

                if (logEventFilter.Count > 0 && transaction.LogEvents.Any(logEvent =>
                        logEventFilter.Contains(logEvent.ContractAddress + logEvent.EventName)))
                {
                    filteredTransactions.Add(transaction);
                }
            }

            block.Transactions = filteredTransactions;
        }

        return blocks;
    }

    public async Task<List<BlockWithTransactionDto>> FilterIncompleteBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks)
    {
        var filteredBlocks = new List<BlockWithTransactionDto>();

        foreach (var block in blocks)
        {
            if (block.Transactions.Count != block.TransactionIds.Count)
            {
                _logger.LogWarning(
                    "Wrong Transactions: block hash {BlockHash}, block height {BlockHeight}, transaction count {TransactionCount}",
                    block.BlockHash, block.BlockHeight, block.Transactions.Count);
                break;
            }

            filteredBlocks.Add(block);
        }

        return filteredBlocks;
    }

    public async Task<List<BlockWithTransactionDto>> FilterIncompleteConfirmedBlocksAsync(string chainId,
        List<BlockWithTransactionDto> blocks, string previousBlockHash, long previousBlockHeight)
    {
        var filteredBlocks = new List<BlockWithTransactionDto>();

        foreach (var block in blocks)
        {
            if (block.PreviousBlockHash != previousBlockHash && previousBlockHash != null ||
                block.BlockHeight != previousBlockHeight + 1)
            {
                _logger.LogWarning(
                    "Wrong confirmed previousBlockHash or previousBlockHash: block hash {BlockHash}, block height {BlockHeight}",
                    block.BlockHash, block.BlockHeight);
                break;
            }

            if (block.Transactions.Count != block.TransactionIds.Count)
            {
                _logger.LogWarning(
                    "Wrong confirmed Transactions: block hash {BlockHash}, block height {BlockHeight}, transaction count {TransactionCount}",
                    block.BlockHash, block.BlockHeight, block.Transactions.Count);
                break;
            }

            filteredBlocks.Add(block);

            previousBlockHash = block.BlockHash;
            previousBlockHeight = block.BlockHeight;
        }

        return filteredBlocks;
    }
}