using AeFinder.App.Es;
using AeFinder.Organizations;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class OrganizationUpdateHandler: IDistributedEventHandler<OrganizationUnitExtensionUpdateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<OrganizationIndex, string> _entityMappingRepository;

    public OrganizationUpdateHandler(IEntityMappingRepository<OrganizationIndex, string> entityMappingRepository)
    {
        _entityMappingRepository = entityMappingRepository;
    }

    public async Task HandleEventAsync(OrganizationUnitExtensionUpdateEto eventData)
    {
        var organizationIndex = await _entityMappingRepository.GetAsync(eventData.OrganizationId.ToString());
        organizationIndex.Status = (int)eventData.OrganizationStatus;
        await _entityMappingRepository.UpdateAsync(organizationIndex);
    }
}