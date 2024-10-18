using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AeFinder.Grains.Grain.BlockPush;

public partial class BlockPusherGrain
{
    private Task<FlowBehavior> HandleHistoricalBlockExceptionAsync(Exception exception)
    {
        // Log the exception information when processing historical blocks.
        _logger.LogError(exception, "Grain: {GrainId} token: {PushToken} handle historical block failed: {Message}",
            this.GetPrimaryKeyString(), State.PushToken, exception.Message);
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow
        });
    }
}