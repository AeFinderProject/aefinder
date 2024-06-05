using AeFinder.App.OperationLimits;
using AeFinder.Block.Dtos;
using AeFinder.BlockScan;
using AeFinder.Sdk.Processor;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AeFinder.App.BlockProcessing;

public interface IFullBlockProcessor
{
    Task ProcessAsync(AppSubscribedBlockDto block, bool isRollback);
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

    public async Task ProcessAsync(AppSubscribedBlockDto block, bool isRollback)
    {
        _operationLimitManager.ResetAll();
        _blockProcessingContext.SetContext(block.ChainId, block.BlockHash, block.BlockHeight,
            block.BlockTime, isRollback);
        
        var blockProcessor = _blockProcessors.FirstOrDefault();
        if (blockProcessor != null)
        {
            _logger.LogInformation(
                "Processing block. ChainId: {ChainId}, BlockHash: {BlockHash}.", block.ChainId, block.BlockHash);
            try
            {
                await blockProcessor.ProcessAsync(_objectMapper.Map<AppSubscribedBlockDto, Sdk.Processor.Block>(block), new BlockContext
                {
                    ChainId = block.ChainId
                });
            }
            catch (Exception e)
            {
                throw new AppProcessingException("Block processing failed!",e);
            }
        }

        var transactionProcessor = _transactionProcessors.FirstOrDefault();
        foreach (var transaction in block.Transactions)
        {
            if (transactionProcessor != null)
            {
                _logger.LogInformation(
                    "Processing transaction. ChainId: {ChainId}, BlockHash: {BlockHash}, TransactionHash: {TransactionHash}.",
                    block.ChainId, block.BlockHash, transaction.TransactionId);
                try
                {
                    await transactionProcessor.ProcessAsync(
                        _objectMapper.Map<AppSubscribedTransactionDto, Transaction>(transaction),
                        new TransactionContext
                        {
                            ChainId = block.ChainId,
                            Block = _objectMapper.Map<AppSubscribedBlockDto, LightBlock>(block)
                        });
                }
                catch (Exception e)
                {
                    throw new AppProcessingException("Transaction processing failed!",e);
                }
            }

            foreach (var logEvent in transaction.LogEvents)
            {
                var logEventProcessor = _logEventProcessors.FirstOrDefault(p =>
                    p.GetContractAddress(block.ChainId) == logEvent.ContractAddress &&
                    p.GetEventName() == logEvent.EventName);
                
                if (logEventProcessor != null)
                {
                    _logger.LogInformation(
                        "Processing log event. ChainId: {ChainId}, BlockHash: {BlockHash}, TransactionHash: {TransactionHash}, ContractAddress: {ContractAddress}, EventName: {EventName}.",
                        block.ChainId, block.BlockHash, transaction.TransactionId, logEvent.ContractAddress,
                        logEvent.EventName);
                    try
                    {
                        await logEventProcessor.ProcessAsync(new LogEventContext
                        {
                            ChainId = block.ChainId,
                            Block = _objectMapper.Map<AppSubscribedBlockDto, LightBlock>(block),
                            Transaction = _objectMapper.Map<AppSubscribedTransactionDto, Transaction>(transaction),
                            LogEvent = _objectMapper.Map<AppSubscribedLogEventDto, LogEvent>(logEvent)
                        });
                    }
                    catch (Exception e)
                    {
                        throw new AppProcessingException("Log event processing failed!",e);
                    }
                }
            }
        }
    }
}