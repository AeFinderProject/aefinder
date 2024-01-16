using AElf.CSharp.Core;

namespace AElfIndexer.Sdk;

public interface ILogEventProcessor
{
    string GetContractAddress(string chainId);
    string GetEventName();
    Task ProcessAsync(LogEventContext context);
}

public interface ILogEventProcessor<TEvent> : ILogEventProcessor
{
    Task ProcessAsync(TEvent logEvent, LogEventContext context);
}