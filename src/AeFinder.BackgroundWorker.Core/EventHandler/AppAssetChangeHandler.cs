using AeFinder.ApiKeys;
using AeFinder.AppResources;
using AeFinder.Apps;
using AeFinder.Apps.Dto;
using AeFinder.Assets;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Merchandises;
using AeFinder.Merchandises;
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
    private readonly IAppResourceService _appResourceService;
    private readonly PodResourceOptions _podResourceOptions;
    private readonly IClusterClient _clusterClient;
    private readonly CustomOrganizationOptions _customOrganizationOptions;
    private readonly IApiKeyService _apiKeyService;

    public AppAssetChangeHandler(ILogger<AppAssetChangeHandler> logger,
        IAppResourceService appResourceService,
        IOptionsSnapshot<PodResourceOptions> podResourceLevelOptions,
        IClusterClient clusterClient,
        IOptionsSnapshot<CustomOrganizationOptions> customOrganizationOptions,
        IApiKeyService apiKeyService,
        IAssetService assetService)
    {
        _logger = logger;
        _assetService = assetService;
        _appResourceService = appResourceService;
        _podResourceOptions = podResourceLevelOptions.Value;
        _clusterClient = clusterClient;
        _customOrganizationOptions = customOrganizationOptions.Value;
        _apiKeyService = apiKeyService;
    }
    
    public async Task HandleEventAsync(AppAssetChangedEto eventData)
    {
        var appId = eventData.AppId;
        foreach (var changedAsset in eventData.ChangedAssets)
        {
            var now = DateTime.UtcNow;
            if (changedAsset.OriginalAsset != null)
            {
                //Release old asset
                var originalAssetId = changedAsset.OriginalAsset.Id;
                await _assetService.ReleaseAssetAsync(originalAssetId, now);
            }

            //Set & Update app resource config
            var newAssetId = changedAsset.Asset.Id;
            var newAssetMerchandiseId = changedAsset.Asset.MerchandiseId;
            //Get merchandise info by Id
            var merchandiseGrain =
                _clusterClient.GetGrain<IMerchandiseGrain>(newAssetMerchandiseId);
            var merchandise = await merchandiseGrain.GetAsync();
            if (merchandise.Type == MerchandiseType.ApiQuery)
            {
                await _assetService.StartUsingAssetAsync(newAssetId, now);
                await _apiKeyService.SetQueryLimitAsync(changedAsset.Asset.OrganizationId, changedAsset.Asset.Quantity);
            }

            if (merchandise.Type == MerchandiseType.Processor)
            {
                var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
                var appDto = await appGrain.GetAsync();
                if (appDto.DeployTime == null && !_customOrganizationOptions.CustomApps.Contains(appId))
                {
                    continue;
                }
                var merchandiseName = merchandise.Specification;
                var resourceInfo = _podResourceOptions.FullPodResourceInfos.Find(r => r.ResourceName == merchandiseName);
            
                await _appResourceService.SetAppResourceLimitAsync(appId, new SetAppResourceLimitDto()
                {
                    AppFullPodLimitCpuCore = resourceInfo.Cpu,
                    AppFullPodLimitMemory = resourceInfo.Memory
                });
                await appGrain.SetFirstDeployTimeAsync(now);
                //Start use new asset
                await _assetService.StartUsingAssetAsync(newAssetId, now);
            }

            if (merchandise.Type == MerchandiseType.Storage)
            {
                var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(appId));
                var appDto = await appGrain.GetAsync();
                if (appDto.DeployTime == null && !_customOrganizationOptions.CustomApps.Contains(appId))
                {
                    continue;
                }
                await appGrain.SetFirstDeployTimeAsync(now);
                await _assetService.StartUsingAssetAsync(newAssetId, now);
            }
            
            
        }
        
    }
}