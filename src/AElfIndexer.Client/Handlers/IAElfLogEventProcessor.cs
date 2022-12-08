using AElfIndexer.Grains.State.Client;

namespace AElfIndexer.Client.Handlers;

public interface IAElfLogEventProcessor<T>
{
    Task HandleEventAsync(LogEventInfo logEventInfo, LogEventContext context = null);
    string GetEventName();
    string GetContractAddress();
}