using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.Apps;
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

public class AppUpgradeHandler : AppHandlerBase,IDistributedEventHandler<AppUpgradeEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IAppDeployManager _kubernetesAppManager;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _appSubscriptionEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodEntityMappingRepository;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAppResourceLimitProvider _appResourceLimitProvider;

    public AppUpgradeHandler(IAppDeployManager kubernetesAppManager,
        IOrganizationAppService organizationAppService,IClusterClient clusterClient,
        IAppResourceLimitProvider appResourceLimitProvider,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionIndex, string> appSubscriptionEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodEntityMappingRepository)
    {
        _clusterClient = clusterClient;
        _organizationAppService = organizationAppService;
        _kubernetesAppManager = kubernetesAppManager;
        _appResourceLimitProvider = appResourceLimitProvider;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _appSubscriptionEntityMappingRepository = appSubscriptionEntityMappingRepository;
        _appSubscriptionPodEntityMappingRepository = appSubscriptionPodEntityMappingRepository;
    }

    public async Task HandleEventAsync(AppUpgradeEto eventData)
    {
        Logger.LogInformation("[AppUpgradeHandler] Start upgrade appId: {0}, pendingVersion: {1} currentVersion: {2}",
            eventData.AppId, eventData.PendingVersion, eventData.CurrentVersion);
        
        var appId = eventData.AppId;
        var historyVersion = eventData.CurrentVersion;
        
        //destory old version pod
        await _appResourceLimitProvider.SetAppPodOperationSnapshotAsync(appId, eventData.CurrentVersion,
            AppPodOperationType.Stop);
        await _kubernetesAppManager.DestroyAppAsync(appId, eventData.CurrentVersion, eventData.CurrentVersionChainIds);
        
        //update app info index
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
        appInfoIndex.Versions = new AppVersionInfo();
        appInfoIndex.Versions.CurrentVersion = eventData.PendingVersion;
        appInfoIndex.Versions.PendingVersion = String.Empty;
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);

        //clear app subscription index
        await _appSubscriptionEntityMappingRepository.DeleteAsync(historyVersion);

        //clear app pod index
        await _appSubscriptionPodEntityMappingRepository.DeleteAsync(historyVersion);

        await AppAttachmentService.DeleteAllAppAttachmentsAsync(eventData.AppId, historyVersion);
        
        //clear old version grain data
        await ClearStoppedVersionAppDataAsync(appId, historyVersion,
            eventData.CurrentVersionChainIds);
    }
}