using AeFinder.BlockScan;
using AElf.ExceptionHandler;
using AElf.OpenTelemetry.ExecutionTime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.App.Handlers;

[AggregateExecutionTime]
public partial class SubscribedBlockHandler : ISubscribedBlockHandler, ISingletonDependency
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
            if (await PublishMessageToEventBusAsync(subscribedBlock, retryCount))
            {
                break;
            }
            retryCount++;
        }
    }

    [ExceptionHandler([typeof(Exception)], TargetType = typeof(SubscribedBlockHandler),
        MethodName = nameof(HandleSubscribedBlockExceptionAsync))]
    protected virtual async Task<bool> PublishMessageToEventBusAsync(SubscribedBlockDto subscribedBlock, int retryCount)
    {
        if (subscribedBlock.Blocks.First().BlockHeight >= 8297246 &&
            subscribedBlock.Blocks.First().BlockHeight <= 8297446)
        {
            throw new Exception("Test Excepiton");
        }

        await _distributedEventBus.PublishAsync(subscribedBlock);
        return true;
    }
}