using AeFinder.App.BlockProcessing;
using AeFinder.App.OperationLimits;
using AeFinder.Apps;
using AeFinder.BlockScan;
using AElf.OpenTelemetry.ExecutionTime;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.App.Handlers;

[AggregateExecutionTime]
public class LocalSubscribedBlockHandler : IDistributedEventHandler<SubscribedBlockDto>, ITransientDependency
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IProcessingStatusProvider _processingStatusProvider;
    private readonly IBlockAttachService _blockAttachService;
    private readonly IVersionUpgradeProvider _versionUpgradeProvider;
    private readonly ILogger<LocalSubscribedBlockHandler> _logger;
    private readonly IAppInfoProvider _appInfoProvider;

    public LocalSubscribedBlockHandler(ILogger<LocalSubscribedBlockHandler> logger,
        IBlockScanAppService blockScanAppService,
        IProcessingStatusProvider processingStatusProvider, IBlockAttachService blockAttachService,
        IVersionUpgradeProvider versionUpgradeProvider, IAppInfoProvider appInfoProvider)
    {
        _logger = logger;
        _blockScanAppService = blockScanAppService;
        _processingStatusProvider = processingStatusProvider;
        _blockAttachService = blockAttachService;
        _versionUpgradeProvider = versionUpgradeProvider;
        _appInfoProvider = appInfoProvider;
    }

    public virtual async Task HandleEventAsync(SubscribedBlockDto subscribedBlock)
    {
        if (!_processingStatusProvider.IsRunning(subscribedBlock.ChainId))
        {
            return;
        }

        if (!_appInfoProvider.ChainId.IsNullOrWhiteSpace() && subscribedBlock.ChainId != _appInfoProvider.ChainId)
        {
            return;
        }

        // TODO: Maybe no need check from silo
        var isRunning = await _blockScanAppService.IsRunningAsync(subscribedBlock.ChainId, subscribedBlock.AppId,
            subscribedBlock.Version, subscribedBlock.PushToken);
        if (!isRunning)
        {
            _logger.LogTrace(
                "DAppSubscribedDataHandler Version is not running! subscribedClientId: {subscribedClientId} subscribedVersion: {subscribedVersion} , ChainId: {ChainId}, Block height: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
                subscribedBlock.AppId, subscribedBlock.Version,
                subscribedBlock.Blocks.First().ChainId,
                subscribedBlock.Blocks.First().BlockHeight,
                subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);
            return;
        }

        _logger.LogInformation(
            AeFinderApplicationConsts.AppLogEventId,
            "Processing blocks. ChainId: {ChainId}, BlockHeight: {FirstBlockHeight}-{LastBlockHeight}, Confirmed: {Confirmed}",
            subscribedBlock.Blocks.First().ChainId, subscribedBlock.Blocks.First().BlockHeight,
            subscribedBlock.Blocks.Last().BlockHeight, subscribedBlock.Blocks.First().Confirmed);

        try
        {
            await _blockAttachService.AttachBlocksAsync(subscribedBlock.ChainId, subscribedBlock.Blocks);
            await _versionUpgradeProvider.UpgradeAsync();
        }
        catch (OperationLimitException e)
        {
            _logger.LogError(AeFinderApplicationConsts.AppLogEventId, e, "[{ChainId}] Data processing failed!",
                subscribedBlock.ChainId);
            _processingStatusProvider.SetStatus(subscribedBlock.AppId, subscribedBlock.Version, 
                subscribedBlock.ChainId, ProcessingStatus.OperationLimited);
        }
        catch (AppProcessingException e)
        {
            _logger.LogError(AeFinderApplicationConsts.AppLogEventId, e, "[{ChainId}] Data processing failed!",
                subscribedBlock.ChainId);
            _processingStatusProvider.SetStatus(subscribedBlock.AppId, subscribedBlock.Version,
                subscribedBlock.ChainId, ProcessingStatus.Failed);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[{ChainId}] Data processing failed!", subscribedBlock.ChainId);
            _logger.LogError(AeFinderApplicationConsts.AppLogEventId, null,
                "[{ChainId}] Data processing failed, please contact the AeFinder!", subscribedBlock.ChainId);
            _processingStatusProvider.SetStatus(subscribedBlock.AppId, subscribedBlock.Version,
                subscribedBlock.ChainId, ProcessingStatus.Failed);
        }
    }
}