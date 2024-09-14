using AeFinder.App.Es;
using AeFinder.User;
using AeFinder.User.Eto;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class MaxAppCountUpdateHandler: IDistributedEventHandler<MaxAppCountUpdateEto>, ITransientDependency
{
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IEntityMappingRepository<OrganizationIndex, string> _entityMappingRepository;

    public MaxAppCountUpdateHandler(IOrganizationAppService organizationAppService,
        IEntityMappingRepository<OrganizationIndex, string> entityMappingRepository)
    {
        _organizationAppService = organizationAppService;
        _entityMappingRepository = entityMappingRepository;
    }

    public async Task HandleEventAsync(MaxAppCountUpdateEto eventData)
    {
        Guid organizationUnitGuid;
        if (!Guid.TryParse(eventData.OrganizationId, out organizationUnitGuid))
        {
            throw new Exception($"Invalid OrganizationUnitId string: {eventData.OrganizationId}");
        }

        var organizationUnitDto = await _organizationAppService.GetOrganizationUnitAsync(organizationUnitGuid);

        var organizationIndex = new OrganizationIndex();
        organizationIndex.OrganizationId = organizationUnitGuid.ToString();
        organizationIndex.OrganizationName = organizationUnitDto.DisplayName;
        organizationIndex.MaxAppCount = eventData.MaxAppCount;
        await _entityMappingRepository.AddOrUpdateAsync(organizationIndex);
    }
}