using AElf.CSharp.Core;

namespace AElfIndexer.Sdk;

public abstract class LogEventProcessorBase<TEvent> : BlockDataProcessorBase, ILogEventProcessor<TEvent>
    where TEvent : IEvent<TEvent>, new()
{
    private readonly string _eventName = typeof(TEvent).Name;

    public string GetEventName()
    {
        return _eventName;
    }
    
    public abstract string GetContractAddress(string chainId);
    
    public async Task ProcessAsync(LogEventContext context)
    {
        var @event = LogEventDeserializationHelper.DeserializeLogEvent<TEvent>(context.LogEvent);
        await ProcessAsync(@event, context);
    }

    public abstract Task ProcessAsync(TEvent logEvent, LogEventContext context);
}