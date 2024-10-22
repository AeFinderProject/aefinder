using AeFinder.App.OperationLimits;
using AeFinder.Apps;
using AeFinder.BlockScan;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AeFinder.App.Handlers;

public partial class LocalSubscribedBlockHandler
{
    private Task<FlowBehavior> HandleBlocksOperationLimitExceptionAsync(OperationLimitException exception,
        SubscribedBlockDto subscribedBlock)
    {
        _logger.LogError(AeFinderApplicationConsts.AppLogEventId, exception, "[{ChainId}] Data processing failed!",
            subscribedBlock.ChainId);
        _processingStatusProvider.SetStatus(subscribedBlock.AppId, subscribedBlock.Version, 
            subscribedBlock.ChainId, ProcessingStatus.OperationLimited);
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        });
    }

    private Task<FlowBehavior> HandleBlocksAppProcessingExceptionAsync(AppProcessingException exception,
        SubscribedBlockDto subscribedBlock)
    {
        _logger.LogError(AeFinderApplicationConsts.AppLogEventId, exception, "[{ChainId}] Data processing failed!",
            subscribedBlock.ChainId);
        _processingStatusProvider.SetStatus(subscribedBlock.AppId, subscribedBlock.Version,
            subscribedBlock.ChainId, ProcessingStatus.Failed);
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        });
    }

    private Task<FlowBehavior> HandleBlocksExceptionAsync(Exception exception, SubscribedBlockDto subscribedBlock)
    {
        // When processing data, there are programs that handle exceptions themselves.
        // If it continues to be thrown, it will cause EventBus to re-enqueue, which makes no sense due to the wrong order.
        _logger.LogError(exception, "[{ChainId}] Data processing failed!", subscribedBlock.ChainId);
        _logger.LogError(AeFinderApplicationConsts.AppLogEventId, null,
            "[{ChainId}] Data processing failed, please contact the AeFinder!", subscribedBlock.ChainId);
        _processingStatusProvider.SetStatus(subscribedBlock.AppId, subscribedBlock.Version,
            subscribedBlock.ChainId, ProcessingStatus.Failed);
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return
        });
    }
}