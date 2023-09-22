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

    public virtual async Task HandleEventAsync(LogEventInfo logEventInfo, LogEventContext context)
    {
        try
        {
            var value = AElfLogEventDeserializationHelper.DeserializeAElfLogEvent<TEvent>(logEventInfo);
            await HandleEventAsync(value, context);
        }
        catch (Exception e)
        {
            throw new DAppHandlingException(
                $"Handle LogEvent Error! ChainId: {context.ChainId} BlockHeight: {context.BlockHeight} BlockHash: {context.BlockHash} TransactionId: {context.TransactionId}  ContractAddress: {logEventInfo.ContractAddress} EventName: {logEventInfo.EventName}.",
                e);
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