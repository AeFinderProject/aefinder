using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppUpgradeHandler : AppHandlerBase,IDistributedEventHandler<AppUpgradeEto>, ITransientDependency
{
    private readonly IAppDeployManager _kubernetesAppManager;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _appSubscriptionEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodEntityMappingRepository;

    public AppUpgradeHandler(IAppDeployManager kubernetesAppManager,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionIndex, string> appSubscriptionEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodEntityMappingRepository)
    {
        _kubernetesAppManager = kubernetesAppManager;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _appSubscriptionEntityMappingRepository = appSubscriptionEntityMappingRepository;
        _appSubscriptionPodEntityMappingRepository = appSubscriptionPodEntityMappingRepository;
    }

    public async Task HandleEventAsync(AppUpgradeEto eventData)
    {
        Logger.LogInformation("[AppUpgradeHandler] Start upgrade appId: {0}, pendingVersion: {1} currentVersion: {2}",
            eventData.AppId, eventData.PendingVersion, eventData.CurrentVersion);
        
        var appId = eventData.AppId;
        var historyVersion = eventData.CurrentVersion;
        
        //destory old version pod
        await _kubernetesAppManager.DestroyAppAsync(appId, eventData.CurrentVersion);

        //clear old version grain data
        await ClearStoppedVersionAppDataAsync(appId, historyVersion,
            eventData.CurrentVersionChainIds);
        
        //update app info index
        var appInfoIndex = await _appInfoEntityMappingRepository.GetAsync(appId);
        if (appInfoIndex == null)
        {
            Logger.LogError($"[AppUpgradeHandler]App {appId} info is missing.");
            appInfoIndex = new AppInfoIndex();
            appInfoIndex.AppId = appId;
        }
        appInfoIndex.Versions.CurrentVersion = eventData.PendingVersion;
        appInfoIndex.Versions.PendingVersion = String.Empty;
        await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);

        //clear app subscription index
        var appSubscriptionIndex = await _appSubscriptionEntityMappingRepository.GetAsync(historyVersion);
        if (appSubscriptionIndex != null)
        {
            await _appSubscriptionEntityMappingRepository.DeleteAsync(appSubscriptionIndex.Id);
        }

        //clear app pod index
        var appSubscriptionPodIndex = await _appSubscriptionPodEntityMappingRepository.GetAsync(historyVersion);
        if (appSubscriptionIndex != null)
        {
            await _appSubscriptionPodEntityMappingRepository.DeleteAsync(appSubscriptionPodIndex.Id);
        }
    }
}