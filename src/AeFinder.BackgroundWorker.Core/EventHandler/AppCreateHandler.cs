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
        //Update organization app ids
        var organizationIndex = new OrganizationIndex();
        
        Guid organizationUnitGuid;
        if (!Guid.TryParse(eventData.OrganizationId, out organizationUnitGuid))
        {
            throw new Exception($"Invalid OrganizationUnitId string: {eventData.OrganizationId}");
        }

        var organizationUnitDto = await _organizationAppService.GetOrganizationUnitAsync(organizationUnitGuid);
        organizationIndex.OrganizationId = organizationUnitGuid.ToString();
        organizationIndex.OrganizationName = organizationUnitDto.DisplayName;
        
        var organizationAppGrain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(GrainIdHelper.GenerateOrganizationAppGrainId(organizationUnitDto.Id));
        var maxAppCount = await organizationAppGrain.GetMaxAppCountAsync();
        organizationIndex.MaxAppCount = maxAppCount;
        var appIds = await organizationAppGrain.GetAppsAsync();
        if (organizationIndex.AppIds == null)
        {
            organizationIndex.AppIds = new List<string>();
        }
        organizationIndex.AppIds = appIds.ToList();
        await _organizationEntityMappingRepository.AddOrUpdateAsync(organizationIndex);

        //Add app info index
        var appInfoIndex = ObjectMapper.Map<AppCreateEto, AppInfoIndex>(eventData);
        appInfoIndex.OrganizationName = organizationIndex.OrganizationName;
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
    }
}