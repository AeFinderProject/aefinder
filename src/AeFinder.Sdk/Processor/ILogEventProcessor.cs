namespace AeFinder.Sdk.Processor;

public interface ILogEventProcessor : IBlockDataProcessor
{
    string GetContractAddress(string chainId);
    string GetEventName();
    Task ProcessAsync(LogEventContext context);
}

public interface ILogEventProcessor<TEvent> : ILogEventProcessor
{
    Task ProcessAsync(TEvent logEvent, LogEventContext context);
}