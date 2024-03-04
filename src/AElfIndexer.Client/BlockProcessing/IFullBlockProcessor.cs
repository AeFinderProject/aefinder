using AElfIndexer.Block.Dtos;
using AElfIndexer.Client.OperationLimits;
using AElfIndexer.Sdk;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.BlockProcessing;

public interface IFullBlockProcessor
{
    Task ProcessAsync(BlockWithTransactionDto block, bool isRollback);
}

public class FullBlockProcessor : IFullBlockProcessor, ISingletonDependency
{
    private readonly IEnumerable<IBlockProcessor> _blockProcessors;
    private readonly IEnumerable<ITransactionProcessor> _transactionProcessors;
    private readonly IEnumerable<ILogEventProcessor> _logEventProcessors;
    private readonly IObjectMapper _objectMapper;
    private readonly IOperationLimitManager _operationLimitManager;
    private readonly ILogger<FullBlockProcessor> _logger;

    public FullBlockProcessor(IEnumerable<IBlockProcessor> blockProcessors,
        IEnumerable<ITransactionProcessor> transactionProcessors,
        IEnumerable<ILogEventProcessor> logEventProcessors, IObjectMapper objectMapper,
        IOperationLimitManager operationLimitManager, ILogger<FullBlockProcessor> logger)
    {
        _blockProcessors = blockProcessors;
        _transactionProcessors = transactionProcessors;
        _logEventProcessors = logEventProcessors;
        _objectMapper = objectMapper;
        _operationLimitManager = operationLimitManager;
        _logger = logger;
    }

    public async Task ProcessAsync(BlockWithTransactionDto block, bool isRollback)
    {
        _operationLimitManager.ResetAll();
        
        var processingContext = new BlockDataProcessingContext(block.ChainId, block.BlockHash, block.BlockHeight,
            block.PreviousBlockHash, block.BlockTime, isRollback);
        var blockProcessor = _blockProcessors.FirstOrDefault();
        if (blockProcessor != null)
        {
            _logger.LogInformation(
                "Processing block. ChainId: {ChainId}, BlockHash: {BlockHash}.", block.ChainId, block.BlockHash);
            
            blockProcessor.SetProcessingContext(processingContext);
            try
            {
                await blockProcessor.ProcessAsync(_objectMapper.Map<BlockWithTransactionDto, Sdk.Block>(block));
            }
            catch (Exception e)
            {
                throw new AppProcessingException("Block processing failed!",e);
            }
        }

        foreach (var transaction in block.Transactions)
        {
            var transactionProcessor = _transactionProcessors.FirstOrDefault(p =>
                p.GetToAddress(block.ChainId) == transaction.To &&
                (p.GetMethodName(block.ChainId).IsNullOrWhiteSpace() ||
                 p.GetMethodName(block.ChainId) == transaction.MethodName));
            if (transactionProcessor != null)
            {
                _logger.LogInformation(
                    "Processing transaction. ChainId: {ChainId}, BlockHash: {BlockHash}, TransactionHash: {TransactionHash}.",
                    transaction.ChainId, transaction.BlockHash, transaction.TransactionId);
                
                transactionProcessor.SetProcessingContext(processingContext);
                try
                {
                    await transactionProcessor.ProcessAsync(
                        _objectMapper.Map<TransactionDto, Sdk.Transaction>(transaction),
                        new Sdk.TransactionContext
                        {
                            ChainId = block.ChainId,
                            Block = _objectMapper.Map<BlockWithTransactionDto, Sdk.LightBlock>(block)
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
                    p.GetContractAddress(logEvent.ChainId) == logEvent.ContractAddress &&
                    p.GetEventName() == logEvent.EventName);
                if (logEventProcessor != null)
                {
                    _logger.LogInformation(
                        "Processing log event. ChainId: {ChainId}, BlockHash: {BlockHash}, TransactionHash: {TransactionHash}, ContractAddress: {ContractAddress}, EventName: {EventName}.",
                        logEvent.ChainId, logEvent.BlockHash, transaction.TransactionId, logEvent.ContractAddress,
                        logEvent.EventName);
                    
                    logEventProcessor.SetProcessingContext(processingContext);

                    try
                    {
                        await logEventProcessor.ProcessAsync(new Sdk.LogEventContext
                        {
                            ChainId = block.ChainId,
                            Block = _objectMapper.Map<BlockWithTransactionDto, Sdk.LightBlock>(block),
                            Transaction = _objectMapper.Map<TransactionDto, Sdk.Transaction>(transaction),
                            LogEvent = _objectMapper.Map<LogEventDto, Sdk.LogEvent>(logEvent)
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