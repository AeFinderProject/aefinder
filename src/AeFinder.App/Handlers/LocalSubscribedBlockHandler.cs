using AeFinder.App.BlockProcessing;
using AeFinder.BlockScan;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.App.Handlers;

public class LocalSubscribedBlockHandler : IDistributedEventHandler<SubscribedBlockDto>, ITransientDependency
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IProcessingStatusProvider _processingStatusProvider;
    private readonly IBlockAttachService _blockAttachService;
    private readonly IVersionUpgradeProvider _versionUpgradeProvider;
    private readonly ILogger<LocalSubscribedBlockHandler> _logger;

    public LocalSubscribedBlockHandler(ILogger<LocalSubscribedBlockHandler> logger,
        IBlockScanAppService blockScanAppService,
        IProcessingStatusProvider processingStatusProvider, IBlockAttachService blockAttachService,
        IVersionUpgradeProvider versionUpgradeProvider)
    {
        _logger = logger;
        _blockScanAppService = blockScanAppService;
        _processingStatusProvider = processingStatusProvider;
        _blockAttachService = blockAttachService;
        _versionUpgradeProvider = versionUpgradeProvider;
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
            AeFinderApplicationConsts.AppLogEventId,
            "Processing blocks. ChainId: {ChainId}, block height: {FirstBlockHeight}-{LastBlockHeight}, confirmed: {Confirmed}",
            subscribedBlock.Blocks.First().ChainId, subscribedBlock.Blocks.First().BlockHeight,
            subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);

        //test skip block
        if (subscribedBlock.Blocks.First().ChainId.Equals("AELF"))
        {
            if (subscribedBlock.Blocks.First().BlockHeight > 1983200  && subscribedBlock.Blocks.First().BlockHeight < 1983333)
            {
                _logger.LogError("drop block");
                return;
            }
        }
        try
        {
            await _blockAttachService.AttachBlocksAsync(subscribedBlock.ChainId, subscribedBlock.Blocks);
            await _versionUpgradeProvider.UpgradeAsync();
        }
        catch (AppProcessingException e)
        {
            _logger.LogError(AeFinderApplicationConsts.AppLogEventId, e, "Data processing failed!");
            _processingStatusProvider.SetStatus(subscribedBlock.ChainId, ProcessingStatus.Failed);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Data processing failed!");
            _logger.LogError(AeFinderApplicationConsts.AppLogEventId, null,
                "Data processing failed, please contact the AeFinder!");
            _processingStatusProvider.SetStatus(subscribedBlock.ChainId, ProcessingStatus.Failed);
        }
    }
}