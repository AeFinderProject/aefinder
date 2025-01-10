using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppUpdateHandler : AppHandlerBase, IDistributedEventHandler<AppUpdateEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    
    public AppUpdateHandler(IOrganizationAppService organizationAppService,IClusterClient clusterClient,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository)
    {
        _clusterClient = clusterClient;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _organizationAppService = organizationAppService;
    }
    
    public async Task HandleEventAsync(AppUpdateEto eventData)
    {
        var appId = eventData.AppId;
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

        appInfoIndex.Description = eventData.Description;
        appInfoIndex.ImageUrl = eventData.ImageUrl;
        appInfoIndex.SourceCodeUrl = eventData.SourceCodeUrl;
        appInfoIndex.UpdateTime = eventData.UpdateTime;
        appInfoIndex.IsLocked = eventData.IsLocked;

        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
    }
}