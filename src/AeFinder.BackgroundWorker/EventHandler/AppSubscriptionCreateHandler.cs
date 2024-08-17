using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Subscriptions;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppSubscriptionCreateHandler: AppHandlerBase, IDistributedEventHandler<AppSubscriptionCreateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _appSubscriptionEntityMappingRepository;
    private readonly IOrganizationAppService _organizationAppService;
    
    public AppSubscriptionCreateHandler(IOrganizationAppService organizationAppService,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionIndex, string> appSubscriptionEntityMappingRepository)
    {
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _appSubscriptionEntityMappingRepository = appSubscriptionEntityMappingRepository;
        _organizationAppService = organizationAppService;
    }
    
    public async Task HandleEventAsync(AppSubscriptionCreateEto eventData)
    {
        var appId = eventData.AppId;
        string version = eventData.CurrentVersion.IsNullOrEmpty() ? eventData.PendingVersion : eventData.CurrentVersion;
        
        //Update app info index
        var appGrain = ClusterClient.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(eventData.AppId));
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
            ClusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var versions = await subscriptionGrain.GetAllSubscriptionAsync();
        appInfoIndex.Versions = new AppVersionInfo();
        appInfoIndex.Versions.CurrentVersion = versions.CurrentVersion?.Version;
        appInfoIndex.Versions.PendingVersion = versions.PendingVersion?.Version;
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);

        //Add app subscription index
        var subscriptionManifest = new SubscriptionManifest();
        var subscriptionStatus = new SubscriptionStatus();
        if (appInfoIndex.Versions.CurrentVersion == version)
        {
            subscriptionManifest = versions.CurrentVersion.SubscriptionManifest;
            subscriptionStatus = versions.CurrentVersion.Status;
        }

        if (appInfoIndex.Versions.PendingVersion == version)
        {
            subscriptionManifest = versions.PendingVersion.SubscriptionManifest;
            subscriptionStatus = versions.PendingVersion.Status;
        }
        
        var subscriptionManifestInfo =
            ObjectMapper.Map<SubscriptionManifest, SubscriptionManifestInfo>(subscriptionManifest);
        var appSubscriptionIndex = new AppSubscriptionIndex()
        {
            AppId = eventData.AppId,
            Version = version,
            SubscriptionManifest = subscriptionManifestInfo,
            SubscriptionStatus = subscriptionStatus
        };
        await _appSubscriptionEntityMappingRepository.AddOrUpdateAsync(appSubscriptionIndex);
    }
}