using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AeFinder.Subscriptions;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppSubscriptionCreateHandler: AppHandlerBase, IDistributedEventHandler<AppSubscriptionCreateEto>, ITransientDependency
{
    private readonly ISubscriptionAppService _clusterClient;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;

    public AppSubscriptionCreateHandler(
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository)
    {
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
    }
    
    public async Task HandleEventAsync(AppSubscriptionCreateEto eventData)
    {
        
    }
}