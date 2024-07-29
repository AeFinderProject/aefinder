using AeFinder.BlockScan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.App.Handlers;

public class SubscribedBlockHandler : ISubscribedBlockHandler, ISingletonDependency
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly MessageQueueOptions _messageQueueOptions;
    private readonly IAppInfoProvider _appInfoProvider;
    public ILogger<SubscribedBlockHandler> Logger { get; set; }
    private const long UpgradeVersionThreshold = 1000;

    public SubscribedBlockHandler(IBlockScanAppService blockScanAppService,
        IDistributedEventBus distributedEventBus,
        IOptionsSnapshot<MessageQueueOptions> messageQueueOptions,
        IAppInfoProvider appInfoProvider)
    {
        _blockScanAppService = blockScanAppService;
        _distributedEventBus = distributedEventBus;
        _appInfoProvider = appInfoProvider;
        _messageQueueOptions = messageQueueOptions.Value;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken token = null)
    {
        if (subscribedBlock.Blocks.Count == 0) return;
        if (subscribedBlock.AppId != _appInfoProvider.AppId || subscribedBlock.Version != _appInfoProvider.Version) return;
        // var isRunning = await _blockScanAppService.IsRunningAsync(subscribedBlock.ChainId, subscribedBlock.AppId,
        //     subscribedBlock.Version, subscribedBlock.PushToken);
        // if (!isRunning)
        // {
        //     Logger.LogTrace(
        //         "SubscribedBlockHandler Version is not running! subscribedClientId: {subscribedClientId} subscribedVersion: {subscribedVersion} subscribedToken: {subscribedToken} clientId: {clientId} , ChainId: {ChainId}, Block height: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
        //         subscribedBlock.AppId, subscribedBlock.Version, subscribedBlock.PushToken,_appInfoProvider.AppId,
        //         subscribedBlock.Blocks.First().ChainId,
        //         subscribedBlock.Blocks.First().BlockHeight,
        //         subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);
        //     return;
        // }

        await PublishMessageAsync(subscribedBlock);
    }
    
    private async Task PublishMessageAsync(SubscribedBlockDto subscribedBlock)
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