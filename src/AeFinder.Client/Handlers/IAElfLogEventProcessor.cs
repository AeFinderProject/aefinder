using AeFinder.Grains.State.Client;

namespace AeFinder.Client.Handlers;

public interface IAElfLogEventProcessor
{
    Task HandleEventAsync(LogEventInfo logEventInfo, LogEventContext context = null);
    string GetEventName();
    string GetContractAddress(string chainId);
}

public interface IAElfLogEventProcessor<TData> : IAElfLogEventProcessor where TData : BlockChainDataBase
{
    
}