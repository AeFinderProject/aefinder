using AElfIndexer.BlockScan;
using AElfIndexer.Client.Providers;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElfIndexer.Client.Handlers;

public class DAppSubscribedDataHandler: IDistributedEventHandler<SubscribedBlockDto>, ITransientDependency
{
    private readonly IEnumerable<IBlockChainDataHandler> _handlers;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly ILogger<DAppSubscribedDataHandler> _logger;

    public DAppSubscribedDataHandler(IEnumerable<IBlockChainDataHandler> handlers,
        ILogger<DAppSubscribedDataHandler> logger,
        IBlockScanAppService blockScanAppService)
    {
        _handlers = handlers;
        _logger = logger;
        _blockScanAppService = blockScanAppService;
    }

    public async Task HandleEventAsync(SubscribedBlockDto subscribedBlock)
    {
        var isRunning = await _blockScanAppService.IsRunningAsync(subscribedBlock.ChainId, subscribedBlock.ClientId,
            subscribedBlock.Version, subscribedBlock.Token);
        if (!isRunning)
        {
            _logger.LogWarning(
                "DAppSubscribedDataHandler Version is not running! subscribedClientId: {subscribedClientId} subscribedVersion: {subscribedVersion} , ChainId: {subscribedBlock}, Block height: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
                subscribedBlock.ClientId, subscribedBlock.Version,
                subscribedBlock.Blocks.First().ChainId,
                subscribedBlock.Blocks.First().BlockHeight,
                subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);
            return;
        }
        
        _logger.LogInformation(
            "Receive {ClientId} subscribedBlock: Version: {Version}, ChainId: {subscribedBlock}, Block height: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
            subscribedBlock.ClientId,subscribedBlock.Version, subscribedBlock.Blocks.First().ChainId, subscribedBlock.Blocks.First().BlockHeight,
            subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);
        // TODO: Remove BlockFilterType
        var handler = _handlers.First(h => h.FilterType == BlockFilterType.Block);
        try
        {
            await handler.HandleBlockChainDataAsync(subscribedBlock.ChainId, subscribedBlock.ClientId, subscribedBlock.Blocks);
        }
        catch (DAppHandlingException e)
        {
            _logger.LogError(e, "Handle DAppSubscribedData Error! ClientId: {clientId} Version: {version}", subscribedBlock.ClientId, subscribedBlock.Version);
            await _blockScanAppService.PauseAsync(subscribedBlock.ClientId, subscribedBlock.Version);
        }
    }
}