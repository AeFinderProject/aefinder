using AElfIndexer.Client.Providers;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.Chains;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using Serilog;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElfIndexer.Client.Handlers;

public class SubscribedBlockHandler : ISubscribedBlockHandler, ISingletonDependency
{
    private readonly IEnumerable<IBlockChainDataHandler> _handlers;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly DappMessageQueueOptions _dappMessageQueueOptions;
    public ILogger<SubscribedBlockHandler> Logger { get; set; }
    private readonly string _clientId;
    private const long UpgradeVersionThreshold = 1000;

    public SubscribedBlockHandler(IEnumerable<IBlockChainDataHandler> handlers,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider, IBlockScanAppService blockScanAppService,
        IDistributedEventBus distributedEventBus,
        IOptionsSnapshot<DappMessageQueueOptions> dappMessageQueueOptions,
        IClusterClient clusterClient)
    {
        _handlers = handlers;
        _clientId = aelfIndexerClientInfoProvider.GetClientId();
        _blockScanAppService = blockScanAppService;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _dappMessageQueueOptions = dappMessageQueueOptions.Value;
    }

    public async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken token = null)
    {
        if (subscribedBlock.Blocks.Count == 0) return;
        if (subscribedBlock.ClientId != _clientId) return;
        var isRunning = await _blockScanAppService.IsVersionRunningAsync(subscribedBlock.ClientId,
            subscribedBlock.Version, subscribedBlock.Token);
        if (!isRunning)
        {
            Logger.LogInformation(
                "SubscribedBlockHandler Version is not running! subscribedClientId: {subscribedClientId} subscribedVersion: {subscribedVersion} subscribedToken: {subscribedToken} clientId: {clientId} FilterType: {FilterType}, ChainId: {ChainId}, Block height: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
                subscribedBlock.ClientId, subscribedBlock.Version, subscribedBlock.Token,
                subscribedBlock.FilterType, subscribedBlock.Blocks.First().ChainId,
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
        while (retryCount < _dappMessageQueueOptions.RetryTimes)
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
                await Task.Delay(_dappMessageQueueOptions.RetryInterval);

                if (retryCount >= _dappMessageQueueOptions.RetryTimes)
                {
                    throw e;
                }
            }
        }
    }
}