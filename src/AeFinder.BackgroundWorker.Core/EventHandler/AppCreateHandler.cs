using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.User;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppCreateHandler : AppHandlerBase, IDistributedEventHandler<AppCreateEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<OrganizationIndex, string> _organizationEntityMappingRepository;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IOrganizationAppService _organizationAppService;

    public AppCreateHandler(IClusterClient clusterClient,
        IEntityMappingRepository<OrganizationIndex, string> organizationEntityMappingRepository,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IOrganizationAppService organizationAppService)
    {
        _clusterClient = clusterClient;
        _organizationEntityMappingRepository = organizationEntityMappingRepository;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _organizationAppService = organizationAppService;
    }

    public async Task HandleEventAsync(AppCreateEto eventData)
    {
        if (!Guid.TryParse(eventData.OrganizationId, out var organizationUnitGuid))
        {
            throw new Exception($"Invalid OrganizationUnitId string: {eventData.OrganizationId}");
        }
        
        var organizationIndex = await _organizationEntityMappingRepository.GetAsync(organizationUnitGuid.ToString());
        
        var organizationAppGrain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(GrainIdHelper.GenerateOrganizationAppGrainId(organizationUnitGuid));
        var appIds = await organizationAppGrain.GetAppsAsync();
        organizationIndex.AppIds = appIds.ToList();
        await _organizationEntityMappingRepository.UpdateAsync(organizationIndex);

        //Add app info index
        var appInfoIndex = ObjectMapper.Map<AppCreateEto, AppInfoIndex>(eventData);
        appInfoIndex.OrganizationName = organizationIndex.OrganizationName;
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
    }
}