using AeFinder.App.OperationLimits;
using AElf.ExceptionHandler;

namespace AeFinder.App.BlockProcessing;

public partial class FullBlockProcessor
{
    private Task<FlowBehavior> HandleProcessBlockOperationLimitExceptionAsync(OperationLimitException exception)
    {
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new OperationLimitException("Block processing operation limited!", exception)
        });
    }
    
    private Task<FlowBehavior> HandleProcessBlockExceptionAsync(Exception exception)
    {
        // Any exception caused by the logic of the external developer's code is encapsulated as an AppProcessingException and handled separately by the upper level.
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new AppProcessingException("Block processing failed!", exception)
        });
    }
    
    private Task<FlowBehavior> HandleProcessTransactionOperationLimitExceptionAsync(OperationLimitException exception)
    {
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new OperationLimitException("Transaction processing operation limited!", exception)
        });
    }
    
    private Task<FlowBehavior> HandleProcessTransactionExceptionAsync(Exception exception)
    {
        // Any exception caused by the logic of the external developer's code is encapsulated as an AppProcessingException and handled separately by the upper level.
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new AppProcessingException("Transaction processing failed!", exception)
        });
    }
    
    private Task<FlowBehavior> HandleProcessLogEventOperationLimitExceptionAsync(OperationLimitException exception)
    {
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new OperationLimitException("Log event processing operation limited!", exception)
        });
    }
    
    private Task<FlowBehavior> HandleProcessLogEventExceptionAsync(Exception exception)
    {
        // Any exception caused by the logic of the external developer's code is encapsulated as an AppProcessingException and handled separately by the upper level.
        return Task.FromResult(new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new AppProcessingException("Log event processing failed!", exception)
        });
    }
}