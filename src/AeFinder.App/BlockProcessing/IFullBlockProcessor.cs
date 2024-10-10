using AeFinder.App.OperationLimits;
using AeFinder.Block.Dtos;
using AeFinder.Sdk.Processor;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AeFinder.App.BlockProcessing;

public interface IFullBlockProcessor
{
    Task ProcessAsync(BlockWithTransactionDto block);
}

public class FullBlockProcessor : IFullBlockProcessor, ISingletonDependency
{
    private readonly IEnumerable<IBlockProcessor> _blockProcessors;
    private readonly IBlockProcessingContext _blockProcessingContext;
    private readonly IEnumerable<ITransactionProcessor> _transactionProcessors;
    private readonly IEnumerable<ILogEventProcessor> _logEventProcessors;
    private readonly IObjectMapper _objectMapper;
    private readonly IOperationLimitManager _operationLimitManager;
    private readonly ILogger<FullBlockProcessor> _logger;

    public FullBlockProcessor(IEnumerable<IBlockProcessor> blockProcessors,
        IEnumerable<ITransactionProcessor> transactionProcessors,
        IEnumerable<ILogEventProcessor> logEventProcessors, IObjectMapper objectMapper,
        IOperationLimitManager operationLimitManager, ILogger<FullBlockProcessor> logger,
        IBlockProcessingContext blockProcessingContext)
    {
        _blockProcessors = blockProcessors;
        _transactionProcessors = transactionProcessors;
        _logEventProcessors = logEventProcessors;
        _objectMapper = objectMapper;
        _operationLimitManager = operationLimitManager;
        _logger = logger;
        _blockProcessingContext = blockProcessingContext;
    }

    public async Task ProcessAsync(BlockWithTransactionDto block)
    {
        _operationLimitManager.ResetAll();
        _blockProcessingContext.SetContext(block.ChainId, block.BlockHash, block.BlockHeight,
            block.BlockTime);

        var blockProcessor = _blockProcessors.FirstOrDefault();
        if (blockProcessor != null)
        {
            _logger.LogDebug(AeFinderApplicationConsts.AppLogEventId,
                "Processing block. ChainId: {ChainId}, BlockHash: {BlockHash}, BlockHeight: {BlockHeight}.",
                block.ChainId, block.BlockHash, block.BlockHeight);
            await ProcessBlockAsync(blockProcessor, block);
        }

        var transactionProcessor = _transactionProcessors.FirstOrDefault();
        foreach (var transaction in block.Transactions)
        {
            if (transactionProcessor != null)
            {
                _logger.LogDebug(AeFinderApplicationConsts.AppLogEventId,
                    "Processing transaction. ChainId: {ChainId}, BlockHash: {BlockHash}, BlockHeight: {BlockHeight}, TransactionHash: {TransactionHash}.",
                    block.ChainId, block.BlockHash, block.BlockHeight, transaction.TransactionId);
                await ProcessTransactionAsync(transactionProcessor, block, transaction);
            }

            foreach (var logEvent in transaction.LogEvents)
            {
                await ProcesslogEventAsync(block, transaction, logEvent);
            }
        }
    }

    private async Task ProcessBlockAsync(IBlockProcessor blockProcessor, BlockWithTransactionDto block)
    {
        try
        {
            await blockProcessor.ProcessAsync(
                _objectMapper.Map<BlockWithTransactionDto, Sdk.Processor.Block>(block), new BlockContext
                {
                    ChainId = block.ChainId
                });
        }
        catch (OperationLimitException e)
        {
            throw new OperationLimitException("Block processing operation limited!", e);
        }
        catch (Exception e)
        {
            // Any exception caused by the logic of the external developer's code is encapsulated as an AppProcessingException and handled separately by the upper level.
            throw new AppProcessingException("Block processing failed!", e);
        }
    }

    private async Task ProcessTransactionAsync(ITransactionProcessor transactionProcessor, BlockWithTransactionDto block, TransactionDto transaction)
    {
        try
        {
            await transactionProcessor.ProcessAsync(
                _objectMapper.Map<TransactionDto, Transaction>(transaction),
                new TransactionContext
                {
                    ChainId = block.ChainId,
                    Block = _objectMapper.Map<BlockWithTransactionDto, LightBlock>(block)
                });
        }
        catch (OperationLimitException e)
        {
            throw new OperationLimitException("Transaction processing operation limited!", e);
        }
        catch (Exception e)
        {
            // Any exception caused by the logic of the external developer's code is encapsulated as an AppProcessingException and handled separately by the upper level.
            throw new AppProcessingException("Transaction processing failed!", e);
        }
    }

    private async Task ProcesslogEventAsync(BlockWithTransactionDto block, TransactionDto transaction,
        LogEventDto logEvent)
    {
        try
        {
            var logEventProcessor = _logEventProcessors.FirstOrDefault(p =>
                p.GetContractAddress(block.ChainId) == logEvent.ContractAddress &&
                p.GetEventName() == logEvent.EventName);

            if (logEventProcessor == null)
            {
                return;
            }

            _logger.LogDebug(AeFinderApplicationConsts.AppLogEventId,
                "Processing log event. ChainId: {ChainId}, BlockHash: {BlockHash}, BlockHeight: {BlockHeight}, TransactionHash: {TransactionHash}, ContractAddress: {ContractAddress}, EventName: {EventName}.",
                block.ChainId, block.BlockHash, block.BlockHeight, transaction.TransactionId,
                logEvent.ContractAddress,
                logEvent.EventName);

            await logEventProcessor.ProcessAsync(new LogEventContext
            {
                ChainId = block.ChainId,
                Block = _objectMapper.Map<BlockWithTransactionDto, LightBlock>(block),
                Transaction = _objectMapper.Map<TransactionDto, Transaction>(transaction),
                LogEvent = _objectMapper.Map<LogEventDto, LogEvent>(logEvent)
            });
        }
        catch (OperationLimitException e)
        {
            throw new OperationLimitException("Log event processing operation limited!", e);
        }
        catch (Exception e)
        {
            // Any exception caused by the logic of the external developer's code is encapsulated as an AppProcessingException and handled separately by the upper level.
            throw new AppProcessingException("Log event processing failed!", e);
        }
    }
}