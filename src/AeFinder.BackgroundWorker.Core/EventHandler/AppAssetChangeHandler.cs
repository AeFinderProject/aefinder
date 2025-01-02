using AeFinder.AppResources;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Assets;
using AeFinder.Grains.Grain.Merchandises;
using AeFinder.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppAssetChangeHandler: IDistributedEventHandler<AppAssetChangedEto>, ITransientDependency
{
    private readonly ILogger<AppAssetChangeHandler> _logger;
    private readonly IAssetService _assetService;
    private readonly IAppOperationSnapshotProvider _appOperationSnapshotProvider;
    private readonly IAppResourceService _appResourceService;
    private readonly PodResourceLevelOptions _podResourceLevelOptions;
    private readonly IClusterClient _clusterClient;

    public AppAssetChangeHandler(ILogger<AppAssetChangeHandler> logger,
        IAppOperationSnapshotProvider appOperationSnapshotProvider,
        IAppResourceService appResourceService,
        IOptionsSnapshot<PodResourceLevelOptions> podResourceLevelOptions,
        IClusterClient clusterClient,
        IAssetService assetService)
    {
        _logger = logger;
        _assetService = assetService;
        _appOperationSnapshotProvider = appOperationSnapshotProvider;
        _appResourceService = appResourceService;
        _podResourceLevelOptions = podResourceLevelOptions.Value;
        _clusterClient = clusterClient;
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

            //Set & Update app resource config
            var newAssetMerchandiseId = changedAsset.Asset.MerchandiseId;
            //Get merchandise info by Id
            var merchandiseGrain =
                _clusterClient.GetGrain<IMerchandiseGrain>(newAssetMerchandiseId);
            var merchandise = await merchandiseGrain.GetAsync();
            var merchandiseName = merchandise.Name;
            var resourceInfo = _podResourceLevelOptions.FullPodResourceLevels.Find(r => r.LevelName == merchandiseName);
            
            await _appResourceService.SetAppResourceLimitAsync(appId, new SetAppResourceLimitDto()
            {
                AppFullPodLimitCpuCore = resourceInfo.Cpu,
                AppFullPodLimitMemory = resourceInfo.Memory
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