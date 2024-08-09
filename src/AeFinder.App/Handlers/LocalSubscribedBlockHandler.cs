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
        
        //first
        if (subscribedBlock.Blocks.First().ChainId.Equals("AELF"))
        {
            //block
            if (subscribedBlock.Blocks.First().BlockHeight > 4172896  && subscribedBlock.Blocks.First().BlockHeight < 4172999)
            {
                foreach (var blockDt in subscribedBlock.Blocks)
                {
                    if (blockDt.Confirmed)
                    {
                        _logger.LogWarning(AeFinderApplicationConsts.AppLogEventId, "drop first confirm block {0}", blockDt.BlockHeight);
                        return;
                    }
                }
                // _logger.LogWarning(AeFinderApplicationConsts.AppLogEventId, "drop first block {0}", subscribedBlock.Blocks.First().BlockHeight);
                // return;
            }
            
            // if (subscribedBlock.Blocks.First().BlockHeight > 4174696  && subscribedBlock.Blocks.First().BlockHeight < 4174996)
            // {
            //     foreach (var blockDt in subscribedBlock.Blocks)
            //     {
            //         if (!blockDt.Confirmed)
            //         {
            //             _logger.LogWarning(AeFinderApplicationConsts.AppLogEventId, "drop block {0}", blockDt.BlockHeight);
            //             return;
            //         }
            //     }
            //     // _logger.LogWarning(AeFinderApplicationConsts.AppLogEventId, "drop first block {0}", subscribedBlock.Blocks.First().BlockHeight);
            //     // return;
            // }
            
            //confirm
            // foreach (var blockDt in subscribedBlock.Blocks)
            // {
            //     if (blockDt.Confirmed)
            //     {
            //         _logger.LogWarning(AeFinderApplicationConsts.AppLogEventId, "drop first confirm block {0}", blockDt.BlockHeight);
            //         return;
            //     }
            // }
        }
        
        //not first
        if (subscribedBlock.Blocks.First().ChainId.Equals("AELF"))
        {
            //block
            // if (subscribedBlock.Blocks.First().BlockHeight > 4295196  && subscribedBlock.Blocks.First().BlockHeight < 4295396)
            // {
            //     foreach (var blockDt in subscribedBlock.Blocks)
            //     {
            //         if (!blockDt.Confirmed)
            //         {
            //             _logger.LogWarning(AeFinderApplicationConsts.AppLogEventId, "drop block {0}", blockDt.BlockHeight);
            //             return;
            //         }
            //     }
            // }
            
            //confirm
            if (subscribedBlock.Blocks.First().BlockHeight > 4192896  && subscribedBlock.Blocks.First().BlockHeight < 4193096)
            {
                foreach (var blockDt in subscribedBlock.Blocks)
                {
                    if (blockDt.Confirmed)
                    {
                        _logger.LogWarning(AeFinderApplicationConsts.AppLogEventId, "drop confirm block {0}", blockDt.BlockHeight);
                        return;
                    }
                }
            }
           
        }
        
        if (subscribedBlock.Blocks.First().ChainId.Equals("tDVV"))
        {
           
            
            
            //block
            if (subscribedBlock.Blocks.First().BlockHeight > 10572087  && subscribedBlock.Blocks.First().BlockHeight < 10572187)
            {
                foreach (var blockDt in subscribedBlock.Blocks)
                {
                    if (!blockDt.Confirmed)
                    {
                        _logger.LogWarning(AeFinderApplicationConsts.AppLogEventId, "drop block {0}", blockDt.BlockHeight);
                        return;
                    }
                }
            }
            
            //confirm
            if (subscribedBlock.Blocks.First().BlockHeight > 10672087  && subscribedBlock.Blocks.First().BlockHeight < 10672187)
            {
                foreach (var blockDt in subscribedBlock.Blocks)
                {
                    if (blockDt.Confirmed)
                    {
                        _logger.LogWarning(AeFinderApplicationConsts.AppLogEventId, "drop confirm block {0}", blockDt.BlockHeight);
                        return;
                    }
                }
            }
        }
        
        
        
        try
        {
            await _blockAttachService.AttachBlocksAsync(subscribedBlock.ChainId, subscribedBlock.Blocks);
            await _versionUpgradeProvider.UpgradeAsync();
        }
        catch (AppProcessingException e)
        {
            _logger.LogError(AeFinderApplicationConsts.AppLogEventId, e, "[{ChainId}] Data processing failed!",
                subscribedBlock.ChainId);
            _processingStatusProvider.SetStatus(subscribedBlock.ChainId, ProcessingStatus.Failed);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[{ChainId}] Data processing failed!", subscribedBlock.ChainId);
            _logger.LogError(AeFinderApplicationConsts.AppLogEventId, null,
                "[{ChainId}] Data processing failed, please contact the AeFinder!", subscribedBlock.ChainId);
            _processingStatusProvider.SetStatus(subscribedBlock.ChainId, ProcessingStatus.Failed);
        }
    }
}