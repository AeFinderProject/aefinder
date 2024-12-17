using AeFinder.App.Es;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.Market;
using AeFinder.User.Eto;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace AeFinder.BackgroundWorker.EventHandler;

public class OrganizationCreateHandler: IDistributedEventHandler<OrganizationCreateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<OrganizationIndex, string> _entityMappingRepository;
    private readonly IClusterClient _clusterClient;
    private readonly IOrderService _orderService;

    public OrganizationCreateHandler(IClusterClient clusterClient,
        IOrderService orderService,
        IEntityMappingRepository<OrganizationIndex, string> entityMappingRepository)
    {
        _clusterClient = clusterClient;
        _orderService = orderService;
        _entityMappingRepository = entityMappingRepository;
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
        
        //Automatically place an order for a free API query package for the organization.
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var freeProduct = await productsGrain.GetFreeApiQueryCountProductAsync();
        await _orderService.CreateOrderAsync(new CreateOrderDto()
        {
            OrganizationId = organizationIndex.OrganizationId,
            ProductId = freeProduct.ProductId,
            ProductNumber = 1,
            PeriodMonths = 1
        });
    }
}