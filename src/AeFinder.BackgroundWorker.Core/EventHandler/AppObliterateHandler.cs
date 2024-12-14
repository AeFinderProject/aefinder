using AeFinder.App.Es;
using AeFinder.Apps.Eto;
using AElf.EntityMapping.Repositories;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppObliterateHandler: IDistributedEventHandler<AppObliterateEto>, ITransientDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppLimitInfoIndex, string> _appLimitInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppPodInfoIndex, string> _appPodInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppPodOperationSnapshotIndex, string> _appPodOperationSnapshotEntityMappingRepository;
    private readonly IEntityMappingRepository<AppPodUsageDurationIndex, string> _appPodUsageDurationEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _appSubscriptionEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodEntityMappingRepository;
    
    public AppObliterateHandler(IClusterClient clusterClient,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<AppLimitInfoIndex, string> appLimitInfoEntityMappingRepository,
        IEntityMappingRepository<AppPodInfoIndex, string> appPodInfoEntityMappingRepository,
        IEntityMappingRepository<AppPodOperationSnapshotIndex, string> appPodOperationSnapshotEntityMappingRepository,
        IEntityMappingRepository<AppPodUsageDurationIndex, string> appPodUsageDurationEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionIndex, string> appSubscriptionEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodEntityMappingRepository)
    {
        _clusterClient = clusterClient;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _appLimitInfoEntityMappingRepository = appLimitInfoEntityMappingRepository;
        _appPodInfoEntityMappingRepository = appPodInfoEntityMappingRepository;
        _appPodOperationSnapshotEntityMappingRepository = appPodOperationSnapshotEntityMappingRepository;
        _appPodUsageDurationEntityMappingRepository = appPodUsageDurationEntityMappingRepository;
        _appSubscriptionEntityMappingRepository = appSubscriptionEntityMappingRepository;
        _appSubscriptionPodEntityMappingRepository = appSubscriptionPodEntityMappingRepository;
    }

    public async Task HandleEventAsync(AppObliterateEto eventData)
    {
        var queryableAppLimitInfo = await _appLimitInfoEntityMappingRepository.GetQueryableAsync();
        var appLimitInfos = queryableAppLimitInfo.Where(o => o.AppId == eventData.AppId).ToList();
        if (appLimitInfos.Count > 0)
        {
            await _appLimitInfoEntityMappingRepository.DeleteManyAsync(appLimitInfos);
        }
        
        var queryableAppPodInfo = await _appPodInfoEntityMappingRepository.GetQueryableAsync();
        var appPodInfos = queryableAppPodInfo.Where(o => o.AppId == eventData.AppId).ToList();
        if (appPodInfos.Count > 0)
        {
            await _appPodInfoEntityMappingRepository.DeleteManyAsync(appPodInfos);
        }
        
        var queryableAppPodOperationSnapshot = await _appPodOperationSnapshotEntityMappingRepository.GetQueryableAsync();
        var appPodOperationSnapshots = queryableAppPodOperationSnapshot.Where(o => o.AppId == eventData.AppId).ToList();
        if (appPodOperationSnapshots.Count > 0)
        {
            await _appPodOperationSnapshotEntityMappingRepository.DeleteManyAsync(appPodOperationSnapshots);
        }
        
        var queryableAppPodUsageDuration = await _appPodUsageDurationEntityMappingRepository.GetQueryableAsync();
        var appPodUsageDurations = queryableAppPodUsageDuration.Where(o => o.AppId == eventData.AppId).ToList();
        if (appPodUsageDurations.Count > 0)
        {
            await _appPodUsageDurationEntityMappingRepository.DeleteManyAsync(appPodUsageDurations);
        }
        
        var queryableAppSubscription = await _appSubscriptionEntityMappingRepository.GetQueryableAsync();
        var appSubscriptions = queryableAppSubscription.Where(o => o.AppId == eventData.AppId).ToList();
        if (appSubscriptions.Count > 0)
        {
            await _appSubscriptionEntityMappingRepository.DeleteManyAsync(appSubscriptions);
        }
        
        var queryableAppSubscriptionPod = await _appSubscriptionPodEntityMappingRepository.GetQueryableAsync();
        var appSubscriptionPods = queryableAppSubscriptionPod.Where(o => o.AppId == eventData.AppId).ToList();
        if (appSubscriptionPods.Count > 0)
        {
            await _appSubscriptionPodEntityMappingRepository.DeleteManyAsync(appSubscriptionPods);
        }
        
        var queryableAppInfo = await _appInfoEntityMappingRepository.GetQueryableAsync();
        var appInfos = queryableAppInfo.Where(o => o.AppId == eventData.AppId).ToList();
        if (appInfos.Count > 0)
        {
            await _appInfoEntityMappingRepository.DeleteManyAsync(appInfos);
        }
    }
}