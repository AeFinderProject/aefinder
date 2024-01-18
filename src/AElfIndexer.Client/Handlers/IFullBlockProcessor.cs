using AElfIndexer.Block.Dtos;
using AElfIndexer.Sdk;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElfIndexer.Client.Handlers;

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

    public FullBlockProcessor(IEnumerable<IBlockProcessor> blockProcessors,
        IEnumerable<ITransactionProcessor> transactionProcessors,
        IEnumerable<ILogEventProcessor> logEventProcessors, IObjectMapper objectMapper)
    {
        _blockProcessors = blockProcessors;
        _transactionProcessors = transactionProcessors;
        _logEventProcessors = logEventProcessors;
        _objectMapper = objectMapper;
    }

    public async Task ProcessAsync(BlockWithTransactionDto block, bool isRollback)
    {
        var processingContext = new BlockDataProcessingContext(block.ChainId, block.BlockHash, block.BlockHeight,
            block.PreviousBlockHash, block.BlockTime, isRollback);
        var blockProcessor = _blockProcessors.FirstOrDefault();
        if (blockProcessor != null)
        {
            blockProcessor.SetProcessingContext(processingContext);
            await blockProcessor.ProcessAsync(_objectMapper.Map<BlockWithTransactionDto, Sdk.Block>(block));
        }

        foreach (var transaction in block.Transactions)
        {
            var transactionProcessor = _transactionProcessors.FirstOrDefault(p =>
                p.GetToAddress(block.ChainId) == transaction.To &&
                (p.GetMethodName(block.ChainId).IsNullOrWhiteSpace() ||
                 p.GetMethodName(block.ChainId) == transaction.MethodName));
            if (transactionProcessor != null)
            {
                transactionProcessor.SetProcessingContext(processingContext);
                await transactionProcessor.ProcessAsync(_objectMapper.Map<TransactionDto, Sdk.Transaction>(transaction),
                    new Sdk.TransactionContext
                    {
                        ChainId = block.ChainId,
                        Block = _objectMapper.Map<BlockWithTransactionDto, Sdk.LightBlock>(block)
                    });
            }

            foreach (var logEvent in transaction.LogEvents)
            {
                var logEventProcessor = _logEventProcessors.FirstOrDefault(p =>
                    p.GetContractAddress(logEvent.ChainId) == logEvent.ContractAddress &&
                    p.GetEventName() == logEvent.EventName);
                if (logEventProcessor != null)
                {
                    logEventProcessor.SetProcessingContext(processingContext);
                    await logEventProcessor.ProcessAsync(new Sdk.LogEventContext
                    {
                        ChainId = block.ChainId,
                        Block = _objectMapper.Map<BlockWithTransactionDto, Sdk.LightBlock>(block),
                        Transaction = _objectMapper.Map<TransactionDto, Sdk.Transaction>(transaction),
                        LogEvent = _objectMapper.Map<LogEventDto, Sdk.LogEvent>(logEvent)
                    });
                }
            }
        }
    }
}