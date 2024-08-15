using AeFinder.App.Es;
using AeFinder.User.Eto;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class MaxAppCountUpdateHandler: IDistributedEventHandler<MaxAppCountUpdateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<OrganizationIndex, string> _entityMappingRepository;

    public MaxAppCountUpdateHandler(IEntityMappingRepository<OrganizationIndex, string> entityMappingRepository)
    {
        _entityMappingRepository = entityMappingRepository;
    }

    public async Task HandleEventAsync(MaxAppCountUpdateEto eventData)
    {
        var organizationIndex = await _entityMappingRepository.GetAsync(eventData.OrganizationId);
        if (organizationIndex != null)
        {
            organizationIndex.MaxAppCount = eventData.MaxAppCount;
            await _entityMappingRepository.AddOrUpdateAsync(organizationIndex);
        }
    }
}