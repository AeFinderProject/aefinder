using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppPodUpdateHandler: AppHandlerBase, IDistributedEventHandler<AppPodUpdateEto>, ITransientDependency
{
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodEntityMappingRepository;

    public AppPodUpdateHandler(IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodEntityMappingRepository)
    {
        _appSubscriptionPodEntityMappingRepository = appSubscriptionPodEntityMappingRepository;
    }
    
    public async Task HandleEventAsync(AppPodUpdateEto eventData)
    {
        var appSubscriptionPodIndex = new AppSubscriptionPodIndex()
        {
            AppId = eventData.AppId,
            Version = eventData.Version,
            DockerImage = eventData.DockerImage
        };
        await _appSubscriptionPodEntityMappingRepository.AddOrUpdateAsync(appSubscriptionPodIndex);
    }
}