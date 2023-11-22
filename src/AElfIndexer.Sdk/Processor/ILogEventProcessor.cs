namespace AElfIndexer.Sdk.Processor;

public interface ILogEventProcessor<T>
{
    Task ProcessAsync(T logEvent, LogEventContext context);
}