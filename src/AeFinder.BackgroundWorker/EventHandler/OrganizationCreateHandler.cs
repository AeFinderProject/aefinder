using AeFinder.App.Es;
using AeFinder.User.Eto;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class OrganizationCreateHandler: IDistributedEventHandler<OrganizationCreateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<OrganizationIndex, string> _entityMappingRepository;

    public OrganizationCreateHandler(IEntityMappingRepository<OrganizationIndex, string> entityMappingRepository)
    {
        _entityMappingRepository = entityMappingRepository;
    }
    
    public async Task HandleEventAsync(OrganizationCreateEto eventData)
    {
        var organizationIndex = new OrganizationIndex();
        organizationIndex.OrganizationId = eventData.OrganizationId;
        organizationIndex.OrganizationName = eventData.OrganizationName;
        organizationIndex.MaxAppCount = eventData.MaxAppCount;
        await _entityMappingRepository.AddOrUpdateAsync(organizationIndex);
    }
}