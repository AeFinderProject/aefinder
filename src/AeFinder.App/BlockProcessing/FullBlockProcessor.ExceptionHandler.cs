using AeFinder.App.OperationLimits;
using AElf.ExceptionHandler;

namespace AeFinder.App.BlockProcessing;

public partial class FullBlockProcessor
{
    private FlowBehavior HandleProcessBlockOperationLimitException(OperationLimitException exception)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new OperationLimitException("Block processing operation limited!", exception)
        };
    }
    
    private FlowBehavior HandleProcessBlockException(Exception exception)
    {
        // Any exception caused by the logic of the external developer's code is encapsulated as an AppProcessingException and handled separately by the upper level.
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new AppProcessingException("Block processing failed!", exception)
        };
    }
    
    private FlowBehavior HandleProcessTransactionOperationLimitException(OperationLimitException exception)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new OperationLimitException("Transaction processing operation limited!", exception)
        };
    }
    
    private FlowBehavior HandleProcessTransactionException(Exception exception)
    {
        // Any exception caused by the logic of the external developer's code is encapsulated as an AppProcessingException and handled separately by the upper level.
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new AppProcessingException("Transaction processing failed!", exception)
        };
    }
    
    private FlowBehavior HandleProcessLogEventOperationLimitException(OperationLimitException exception)
    {
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new OperationLimitException("Log event processing operation limited!", exception)
        };
    }
    
    private FlowBehavior HandleProcessLogEventException(Exception exception)
    {
        // Any exception caused by the logic of the external developer's code is encapsulated as an AppProcessingException and handled separately by the upper level.
        return new FlowBehavior
        {
            ExceptionHandlingStrategy = ExceptionHandlingStrategy.Throw,
            ReturnValue = new AppProcessingException("Log event processing failed!", exception)
        };
    }
}