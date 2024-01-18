using AElfIndexer.BlockScan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans.Streams;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElfIndexer.Client.Handlers;

public class SubscribedBlockHandler : ISubscribedBlockHandler, ISingletonDependency
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ClientOptions _clientOptions;
    private readonly AppInfoOptions _appInfoOptions;
    public ILogger<SubscribedBlockHandler> Logger { get; set; }
    private const long UpgradeVersionThreshold = 1000;

    public SubscribedBlockHandler(IBlockScanAppService blockScanAppService,
        IDistributedEventBus distributedEventBus,
        IOptionsSnapshot<ClientOptions> clientOptions,
        IOptionsSnapshot<AppInfoOptions> appInfoOptions)
    {
        _blockScanAppService = blockScanAppService;
        _distributedEventBus = distributedEventBus;
        _clientOptions = clientOptions.Value;
        _appInfoOptions = appInfoOptions.Value;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken token = null)
    {
        if (subscribedBlock.Blocks.Count == 0) return;
        if (subscribedBlock.ClientId != _appInfoOptions.ScanAppId) return;
        var isRunning = await _blockScanAppService.IsRunningAsync(subscribedBlock.ChainId, subscribedBlock.ClientId,
            subscribedBlock.Version, subscribedBlock.Token);
        if (!isRunning)
        {
            Logger.LogWarning(
                "SubscribedBlockHandler Version is not running! subscribedClientId: {subscribedClientId} subscribedVersion: {subscribedVersion} subscribedToken: {subscribedToken} clientId: {clientId} , ChainId: {ChainId}, Block height: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
                subscribedBlock.ClientId, subscribedBlock.Version, subscribedBlock.Token,_appInfoOptions.ScanAppId,
                subscribedBlock.Blocks.First().ChainId,
                subscribedBlock.Blocks.First().BlockHeight,
                subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);
            return;
        }

        await PublishMessageAsync(subscribedBlock);


        //TODO: This can only check one chain 
        // if (subscribedBlock.Version == clientVersion.NewVersion)
        // {
        //     var chainGrain = _clusterClient.GetGrain<IChainGrain>(subscribedBlock.ChainId);
        //     var chainStatus = await chainGrain.GetChainStatusAsync();
        //     if (subscribedBlock.Blocks.Last().BlockHeight > chainStatus.BlockHeight - UpgradeVersionThreshold)
        //     {
        //         await _blockScanAppService.UpgradeVersionAsync(subscribedBlock.ClientId);
        //     }
        // }
    }
    
    private async Task PublishMessageAsync(SubscribedBlockDto subscribedBlock)
    {
        var retryCount = 0;
        while (retryCount < _clientOptions.MessageQueue.RetryTimes)
        {
            try
            {
                await _distributedEventBus.PublishAsync(subscribedBlock);
                break;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Publish subscribedBlock event failed, retrying..." + retryCount);
                retryCount++;
                await Task.Delay(_clientOptions.MessageQueue.RetryInterval);

                if (retryCount >= _clientOptions.MessageQueue.RetryTimes)
                {
                    throw e;
                }
            }
        }
    }
}