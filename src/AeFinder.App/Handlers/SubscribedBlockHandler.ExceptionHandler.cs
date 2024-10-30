using AeFinder.BlockScan;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AeFinder.App.Handlers;

public partial class SubscribedBlockHandler
{
    private async Task<FlowBehavior> HandleSubscribedBlockExceptionAsync(Exception exception,
        SubscribedBlockDto subscribedBlock, int retryCount)
    {
        // Any exception will attempt to resend the data as long as it is within the range of retries.
        Logger.LogError(exception, "[{ChainId}] Publish subscribedBlock event failed, retry times: {RetryCount}",
            subscribedBlock.ChainId, retryCount);
        
        if (retryCount +1 >= _messageQueueOptions.RetryTimes)
        {
            return new FlowBehavior
            {
                ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
            };
        }

        await Task.Delay(_messageQueueOptions.RetryInterval);
        
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Return,
            ReturnValue = false
        };
    }
}