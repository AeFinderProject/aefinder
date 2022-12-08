using AElf.CSharp.Core;
using AElfIndexer.Grains.State.Client;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Handlers;

public abstract class AElfLogEventProcessorBase<TEvent,T> : IAElfLogEventProcessor<T>, ISingletonDependency
    where TEvent : IEvent<TEvent>, new()
{
    private readonly string _eventName;
    
    protected AElfLogEventProcessorBase()
    {
        _eventName = typeof(TEvent).Name;
        //_eventName = new TEvent().Descriptor.Name;  //这个更准确
    }

    public virtual async Task HandleEventAsync(LogEventInfo logEventInfo, LogEventContext context = null)
    {
        var value = AElfLogEventDeserializationHelper.DeserializeAElfLogEvent<TEvent>(logEventInfo);
        await HandleEventAsync(value, context);
    }

    public string GetEventName(){
        return _eventName;
    }
 
    public abstract string GetContractAddress();
    
    protected virtual Task HandleEventAsync(TEvent eventValue, LogEventContext context)
    {
        return Task.CompletedTask;
    }
}