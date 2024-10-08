using AeFinder.BlockScan;
using AElf.OpenTelemetry.ExecutionTime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.App.Handlers;

[AggregateExecutionTime]
public class SubscribedBlockHandler : ISubscribedBlockHandler, ISingletonDependency
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly MessageQueueOptions _messageQueueOptions;
    private readonly IAppInfoProvider _appInfoProvider;
    public ILogger<SubscribedBlockHandler> Logger { get; set; }

    public SubscribedBlockHandler(IDistributedEventBus distributedEventBus,
        IOptionsSnapshot<MessageQueueOptions> messageQueueOptions,
        IAppInfoProvider appInfoProvider)
    {
        _distributedEventBus = distributedEventBus;
        _appInfoProvider = appInfoProvider;
        _messageQueueOptions = messageQueueOptions.Value;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken token = null)
    {
        if (subscribedBlock.Blocks.Count == 0)
        {
            return;
        }

        if (subscribedBlock.AppId != _appInfoProvider.AppId || subscribedBlock.Version != _appInfoProvider.Version ||
            (!_appInfoProvider.ChainId.IsNullOrWhiteSpace() && subscribedBlock.ChainId != _appInfoProvider.ChainId))
        {
            return;
        }

        await PublishMessageAsync(subscribedBlock);
    }

    protected virtual async Task PublishMessageAsync(SubscribedBlockDto subscribedBlock)
    {
        var retryCount = 0;
        while (retryCount < _messageQueueOptions.RetryTimes)
        {
            try
            {
                await _distributedEventBus.PublishAsync(subscribedBlock);
                break;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "[{ChainId}] Publish subscribedBlock event failed, retrying..." + retryCount,
                    subscribedBlock.ChainId);
                retryCount++;
                await Task.Delay(_messageQueueOptions.RetryInterval);

                if (retryCount >= _messageQueueOptions.RetryTimes)
                {
                    throw e;
                }
            }
        }
    }
}