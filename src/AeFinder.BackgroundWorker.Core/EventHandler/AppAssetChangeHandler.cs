using AeFinder.AppResources;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Assets;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppAssetChangeHandler: IDistributedEventHandler<AppAssetChangedEto>, ITransientDependency
{
    private readonly ILogger<AppAssetChangeHandler> _logger;
    private readonly IAssetService _assetService;
    private readonly IAppOperationSnapshotProvider _appOperationSnapshotProvider;
    private readonly IAppResourceService _appResourceService;

    public AppAssetChangeHandler(ILogger<AppAssetChangeHandler> logger,
        IAppOperationSnapshotProvider appOperationSnapshotProvider,
        IAppResourceService appResourceService,
        IAssetService assetService)
    {
        _logger = logger;
        _assetService = assetService;
        _appOperationSnapshotProvider = appOperationSnapshotProvider;
        _appResourceService = appResourceService;
    }
    
    public async Task HandleEventAsync(AppAssetChangedEto eventData)
    {
        var appId = eventData.AppId;
        foreach (var changedAsset in eventData.ChangedAssets)
        {
            if (changedAsset.OriginalAsset != null)
            {
                //Release old asset
                var originalAssetId = changedAsset.OriginalAsset.Id;
                await _assetService.ReleaseAssetAsync(originalAssetId, DateTime.UtcNow);
            }

            //TODO Set & Update app resource config
            await _appResourceService.SetAppResourceLimitAsync(appId, new SetAppResourceLimitDto()
            {
                // AppFullPodLimitCpuCore = resourceDto.Capacity.Cpu,
                // AppFullPodLimitMemory = resourceDto.Capacity.Memory
            });
            //If pod never created, then not use asset
            var podStartUseTime = await _appOperationSnapshotProvider.GetAppPodStartTimeAsync(appId);
            if (podStartUseTime == null)
            {
                return;
            }
            //Start use new asset
            var newAssetId = changedAsset.Asset.Id;
            await _assetService.StartUsingAssetAsync(newAssetId, DateTime.UtcNow);
        }
        
    }
}