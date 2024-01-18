using AElfIndexer.BlockScan;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElfIndexer.Client.Handlers;

public class LocalSubscribedBlockHandler: IDistributedEventHandler<SubscribedBlockDto>, ITransientDependency
{
    private readonly IBlockDataHandler _blockDataHandler;
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly ILogger<LocalSubscribedBlockHandler> _logger;

    public LocalSubscribedBlockHandler(ILogger<LocalSubscribedBlockHandler> logger,
        IBlockScanAppService blockScanAppService, IBlockDataHandler blockDataHandler)
    {
        _logger = logger;
        _blockScanAppService = blockScanAppService;
        _blockDataHandler = blockDataHandler;
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

        try
        {
            await _blockDataHandler.HandleBlockChainDataAsync(subscribedBlock.ChainId, subscribedBlock.ClientId, subscribedBlock.Blocks);
        }
        catch (AppHandlingException e)
        {
            _logger.LogError(e, "Handle DAppSubscribedData Error! ClientId: {clientId} Version: {version}", subscribedBlock.ClientId, subscribedBlock.Version);
            await _blockScanAppService.PauseAsync(subscribedBlock.ClientId, subscribedBlock.Version);
        }
    }
}