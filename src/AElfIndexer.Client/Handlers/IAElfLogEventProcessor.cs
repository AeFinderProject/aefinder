using AElfIndexer.Grains.State.Client;

namespace AElfIndexer.Client.Handlers;

public interface IAElfLogEventProcessor
{
    Task HandleEventAsync(LogEventInfo logEventInfo, LogEventContext context);
    string GetEventName();
    string GetContractAddress(string chainId);
}

public interface IAElfLogEventProcessor<TData> : IAElfLogEventProcessor where TData : BlockChainDataBase
{
    
}