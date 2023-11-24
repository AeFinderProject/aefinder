using AElf.CSharp.Core;
using AElfIndexer.Client.Helpers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Handlers;

public abstract class AElfLogEventProcessorBase<TEvent,TData> : IAElfLogEventProcessor<TData>, ISingletonDependency
    where TEvent : IEvent<TEvent>, new()
    where TData : BlockChainDataBase
{
    private readonly string _eventName;
    private readonly ILogger<AElfLogEventProcessorBase<TEvent,TData>> _logger;
    
    protected AElfLogEventProcessorBase(ILogger<AElfLogEventProcessorBase<TEvent,TData>> logger)
    {
        _logger = logger;
        _eventName = typeof(TEvent).Name;
        //_eventName = new TEvent().Descriptor.Name;
    }

    public virtual async Task HandleEventAsync(LogEventInfo logEventInfo, LogEventContext context = null)
    {
        var value = AElfLogEventDeserializationHelper.DeserializeAElfLogEvent<TEvent>(logEventInfo);
        try
        {
            await HandleEventAsync(value, context);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "Handle Client LogEvent Error! ChainId: " + (context == null ? "Unknown" : context.ChainId) +
                " BlockHeight: " + (context == null ? "Unknown" : context.BlockHeight) + " EventName: " + _eventName +
                " ErrorMsg:" + e.Message);
        }
    }

    public string GetEventName(){
        return _eventName;
    }

    public abstract string GetContractAddress(string chainId);
    
    protected virtual Task HandleEventAsync(TEvent eventValue, LogEventContext context)
    {
        return Task.CompletedTask;
    }
}