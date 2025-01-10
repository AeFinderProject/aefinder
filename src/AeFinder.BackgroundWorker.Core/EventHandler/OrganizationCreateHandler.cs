using AeFinder.ApiKeys;
using AeFinder.App.Es;
using AeFinder.Assets;
using AeFinder.Merchandises;
using AeFinder.User.Eto;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Timing;

namespace AeFinder.BackgroundWorker.EventHandler;

public class OrganizationCreateHandler: IDistributedEventHandler<OrganizationCreateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<OrganizationIndex, string> _entityMappingRepository;
    private readonly IAssetInitializationService _assetInitializationService;

    public OrganizationCreateHandler(IEntityMappingRepository<OrganizationIndex, string> entityMappingRepository,
        IAssetInitializationService assetInitializationService)
    {
        _entityMappingRepository = entityMappingRepository;
        _assetInitializationService = assetInitializationService;
    }

    public async Task HandleEventAsync(OrganizationCreateEto eventData)
    {
        Guid organizationUnitGuid;
        if (!Guid.TryParse(eventData.OrganizationId, out organizationUnitGuid))
        {
            throw new Exception($"Invalid OrganizationUnitId string: {eventData.OrganizationId}");
        }

        var organizationIndex = new OrganizationIndex();
        organizationIndex.OrganizationId = organizationUnitGuid.ToString();
        organizationIndex.OrganizationName = eventData.OrganizationName;
        organizationIndex.MaxAppCount = eventData.MaxAppCount;
        await _entityMappingRepository.AddOrUpdateAsync(organizationIndex);

        await _assetInitializationService.InitializeAsync(organizationUnitGuid);
    }
}