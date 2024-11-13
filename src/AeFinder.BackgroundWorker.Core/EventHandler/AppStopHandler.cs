using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppStopHandler : AppHandlerBase,IDistributedEventHandler<AppStopEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IAppDeployManager _kubernetesAppManager;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _appSubscriptionEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodEntityMappingRepository;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAppResourceLimitProvider _appResourceLimitProvider;

    public AppStopHandler(IAppDeployManager kubernetesAppManager,
        IOrganizationAppService organizationAppService,IClusterClient clusterClient,
        IAppResourceLimitProvider appResourceLimitProvider,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionIndex, string> appSubscriptionEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodEntityMappingRepository)
    {
        _clusterClient = clusterClient;
        _kubernetesAppManager = kubernetesAppManager;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _appSubscriptionEntityMappingRepository = appSubscriptionEntityMappingRepository;
        _appSubscriptionPodEntityMappingRepository = appSubscriptionPodEntityMappingRepository;
        _organizationAppService = organizationAppService;
        _appResourceLimitProvider = appResourceLimitProvider;
    }

    public async Task HandleEventAsync(AppStopEto eventData)
    {
        Logger.LogInformation("[AppStopHandler] Start stop appId: {0}, stopVersion: {1}",
            eventData.AppId, eventData.StopVersion);
        
        var appId = eventData.AppId;
        var version = eventData.StopVersion;
        
        //destroy app pod
        await _appResourceLimitProvider.SetAppPodOperationSnapshotAsync(appId, version, AppPodOperationType.Stop);
        await _kubernetesAppManager.DestroyAppAsync(appId, version, eventData.StopVersionChainIds);
        
        //update app info index of stopped version
        var appGrain = _clusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(eventData.AppId));
        var appDto = await appGrain.GetAsync();
        var appInfoIndex = ObjectMapper.Map<AppDto, AppInfoIndex>(appDto);
        
        var organizationId = await appGrain.GetOrganizationIdAsync();
        Guid organizationUnitGuid;
        if (!Guid.TryParse(organizationId, out organizationUnitGuid))
        {
            throw new Exception($"Invalid OrganizationUnitId string: {organizationId}");
        }
        var organizationUnitDto = await _organizationAppService.GetOrganizationUnitAsync(organizationUnitGuid);
        
        appInfoIndex.OrganizationId = organizationId;
        appInfoIndex.OrganizationName = organizationUnitDto.DisplayName;
        var subscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var versions = await subscriptionGrain.GetAllSubscriptionAsync();
        appInfoIndex.Versions = new AppVersionInfo();
        appInfoIndex.Versions.CurrentVersion = versions.CurrentVersion?.Version;
        appInfoIndex.Versions.PendingVersion = versions.PendingVersion?.Version;
        if (appInfoIndex.Versions.CurrentVersion == version || appInfoIndex.Versions.CurrentVersion.IsNullOrEmpty())
        {
            appInfoIndex.Versions.CurrentVersion = "";
        }

        if (appInfoIndex.Versions.PendingVersion == version || appInfoIndex.Versions.PendingVersion.IsNullOrEmpty())
        {
            appInfoIndex.Versions.PendingVersion = "";
        }

        Logger.LogInformation("[AppStopHandler] CurrentVersion: {0},PendingVersion: {1}, stopVersion: {2}",
            appInfoIndex.Versions.CurrentVersion, appInfoIndex.Versions.PendingVersion, version);
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
        Logger.LogInformation("[AppStopHandler] App info index updated: {0}, stopVersion: {1}",
            eventData.AppId, eventData.StopVersion);
        
        //clear app subscription index of stopped version
        await _appSubscriptionEntityMappingRepository.DeleteAsync(version);
        Logger.LogInformation("[AppStopHandler] App subscription index deleted: {0}, stopVersion: {1}",
            eventData.AppId, eventData.StopVersion);
        
        //clear app pod index
        await _appSubscriptionPodEntityMappingRepository.DeleteAsync(version);
        Logger.LogInformation("[AppStopHandler] App pod index deleted: {0}, stopVersion: {1}",
            eventData.AppId, eventData.StopVersion);
        
        await AppAttachmentService.DeleteAllAppAttachmentsAsync(eventData.AppId, eventData.StopVersion);

        //clear stopped version grain data
        await ClearStoppedVersionAppDataAsync(appId, version,
            eventData.StopVersionChainIds);
    }

}