namespace AElfIndexer.Sdk.Processor;

public abstract class LogEventProcessorBase<T> : BlockDataProcessorBase, ILogEventProcessor<T>
{
    public abstract Task ProcessAsync(T logEvent, LogEventContext context);
}