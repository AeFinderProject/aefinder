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
    private readonly IAssetService _assetService;
    private readonly IMerchandiseService _merchandiseService;
    private readonly IClock _clock;
    private readonly IApiKeyService _apiKeyService;

    public OrganizationCreateHandler(IEntityMappingRepository<OrganizationIndex, string> entityMappingRepository,
        IAssetService assetService, IMerchandiseService merchandiseService, IClock clock, IApiKeyService apiKeyService)
    {
        _entityMappingRepository = entityMappingRepository;
        _assetService = assetService;
        _merchandiseService = merchandiseService;
        _clock = clock;
        _apiKeyService = apiKeyService;
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

        await InitAssetAsync(organizationUnitGuid);
    }

    private async Task InitAssetAsync(Guid organizationId)
    {
        // TODO: Add config
        var apiQuery = await _merchandiseService.GetListAsync(new GetMerchandiseInput
        {
            Type = MerchandiseType.ApiQuery
        });

        var time = _clock.Now;
        var freeQuantity = 100000;
        var asset = await _assetService.CreateAsync(organizationId, new CreateAssetInput
        {
            MerchandiseId = apiQuery.Items.First().Id,
            Quantity = freeQuantity,
            Replicas = 1,
            FreeQuantity = freeQuantity,
            FreeReplicas = 1,
            FreeType = AssetFreeType.Permanent,
            CreateTime = time
        });
        await _assetService.StartUsingAssetAsync(asset.Id, time);

        await _apiKeyService.SetQueryLimitAsync(organizationId, freeQuantity);
    }
}