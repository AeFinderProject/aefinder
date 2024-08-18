using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppSubscriptionUpdateHandler: AppHandlerBase, IDistributedEventHandler<AppSubscriptionUpdateEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _appSubscriptionEntityMappingRepository;
    
    public AppSubscriptionUpdateHandler(IClusterClient clusterClient,
        IEntityMappingRepository<AppSubscriptionIndex, string> appSubscriptionEntityMappingRepository)
    {
        _clusterClient = clusterClient;
        _appSubscriptionEntityMappingRepository = appSubscriptionEntityMappingRepository;
    }

    public async Task HandleEventAsync(AppSubscriptionUpdateEto eventData)
    {
        var appSubscriptionIndex = new AppSubscriptionIndex();
        appSubscriptionIndex.AppId = eventData.AppId;
        appSubscriptionIndex.Version = eventData.Version;

        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(eventData.AppId));
        var subscriptionManifest = await appSubscriptionGrain.GetSubscriptionAsync(eventData.Version);
        var subscriptionStatus = await appSubscriptionGrain.GetSubscriptionStatusAsync(eventData.Version);
        var subscriptionManifestInfo =
            ObjectMapper.Map<SubscriptionManifest, SubscriptionManifestInfo>(subscriptionManifest);
        appSubscriptionIndex.SubscriptionManifest = subscriptionManifestInfo;
        appSubscriptionIndex.SubscriptionStatus = subscriptionStatus;
        await _appSubscriptionEntityMappingRepository.AddOrUpdateAsync(appSubscriptionIndex);
    }
}