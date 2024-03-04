using AElfIndexer.App.BlockProcessing;
using AElfIndexer.App.BlockState;
using AElfIndexer.BlockScan;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElfIndexer.App.Handlers;

public class LocalSubscribedBlockHandler : IDistributedEventHandler<SubscribedBlockDto>, ITransientDependency
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IProcessingStatusProvider _processingStatusProvider;
    private readonly IBlockAttachService _blockAttachService;
    private readonly ILogger<LocalSubscribedBlockHandler> _logger;

    public LocalSubscribedBlockHandler(ILogger<LocalSubscribedBlockHandler> logger,
        IBlockScanAppService blockScanAppService,
        IProcessingStatusProvider processingStatusProvider, IBlockAttachService blockAttachService)
    {
        _logger = logger;
        _blockScanAppService = blockScanAppService;
        _processingStatusProvider = processingStatusProvider;
        _blockAttachService = blockAttachService;
    }

    public async Task HandleEventAsync(SubscribedBlockDto subscribedBlock)
    {
        if (!_processingStatusProvider.IsRunning(subscribedBlock.ChainId))
        {
            return;
        }

        // TODO: Maybe no need check from silo
        var isRunning = await _blockScanAppService.IsRunningAsync(subscribedBlock.ChainId, subscribedBlock.AppId,
            subscribedBlock.Version, subscribedBlock.PushToken);
        if (!isRunning)
        {
            _logger.LogWarning(
                "DAppSubscribedDataHandler Version is not running! subscribedClientId: {subscribedClientId} subscribedVersion: {subscribedVersion} , ChainId: {subscribedBlock}, Block height: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
                subscribedBlock.AppId, subscribedBlock.Version,
                subscribedBlock.Blocks.First().ChainId,
                subscribedBlock.Blocks.First().BlockHeight,
                subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);
            return;
        }

        _logger.LogInformation(
            AElfIndexerApplicationConsts.AppLogEventId,
            "Processing blocks. ChainId: {ChainId}, block height: {FirstBlockHeight}-{LastBlockHeight}, confirmed: {Confirmed}",
            subscribedBlock.Blocks.First().ChainId, subscribedBlock.Blocks.First().BlockHeight,
            subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);

        try
        {
            await _blockAttachService.AttachBlocksAsync(subscribedBlock.ChainId, subscribedBlock.Blocks);
        }
        catch (AppProcessingException e)
        {
            _logger.LogError(AElfIndexerApplicationConsts.AppLogEventId, e, "Data processing failed!");
            _processingStatusProvider.SetStatus(subscribedBlock.ChainId, ProcessingStatus.Failed);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Data processing failed!");
            _logger.LogError(AElfIndexerApplicationConsts.AppLogEventId, null,
                "Data processing failed, please contact the AeFinder!");
            _processingStatusProvider.SetStatus(subscribedBlock.ChainId, ProcessingStatus.Failed);
        }
    }
}