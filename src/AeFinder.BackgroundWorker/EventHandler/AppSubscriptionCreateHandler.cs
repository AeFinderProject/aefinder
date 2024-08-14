using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Subscriptions;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppSubscriptionCreateHandler: AppHandlerBase, IDistributedEventHandler<AppSubscriptionCreateEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _appSubscriptionEntityMappingRepository;
    

    public AppSubscriptionCreateHandler(IClusterClient clusterClient,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionIndex, string> appSubscriptionEntityMappingRepository)
    {
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _appSubscriptionEntityMappingRepository = appSubscriptionEntityMappingRepository;
        _clusterClient = clusterClient;
    }
    
    public async Task HandleEventAsync(AppSubscriptionCreateEto eventData)
    {
        string version = eventData.CurrentVersion.IsNullOrEmpty() ? eventData.PendingVersion : eventData.CurrentVersion;
        

        //Update app info index
        var appInfoIndex = await _appInfoEntityMappingRepository.GetAsync(eventData.AppId);
        if (appInfoIndex == null)
        {
            Logger.LogError($"[AppSubscriptionCreateHandler]App {eventData.AppId} info is missing.");
            appInfoIndex = new AppInfoIndex();
            appInfoIndex.AppId = eventData.AppId;
        }
        
        if (!eventData.CurrentVersion.IsNullOrEmpty())
        {
            appInfoIndex.Versions.CurrentVersion = eventData.CurrentVersion;
        }

        if (!eventData.PendingVersion.IsNullOrEmpty())
        {
            appInfoIndex.Versions.PendingVersion = eventData.PendingVersion;
        }

        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);

        //Add app subscription index
        var appSubscriptionGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppGrainId(eventData.AppId));
        var subscriptionManifest = await appSubscriptionGrain.GetSubscriptionAsync(version);
        var subscriptionStatus = await appSubscriptionGrain.GetSubscriptionStatusAsync(version);
        var subscriptionManifestInfo =
            ObjectMapper.Map<SubscriptionManifest, SubscriptionManifestInfo>(subscriptionManifest);
        var appSubscriptionIndex = new AppSubscriptionIndex()
        {
            AppId = eventData.AppId,
            Version = version,
            SubscriptionManifest = subscriptionManifestInfo,
            SubscriptionStatus = subscriptionStatus
        };
        await _appSubscriptionEntityMappingRepository.AddOrUpdateAsync(appSubscriptionIndex);
    }
}