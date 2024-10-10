using System;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;

namespace AeFinder.EntityEventHandler;

public partial class BlockIndexHandler
{
    private FlowBehavior HandleNewBlockException(Exception exception)
    {
        // Log processing exception.
        Logger.LogError(exception, "Process new block failed.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
    
    private FlowBehavior HandleConfirmedBlockException(Exception exception)
    {
        // Log processing exception.
        Logger.LogError(exception, "Process Confirmed block failed.");
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Rethrow,
        };
    }
}