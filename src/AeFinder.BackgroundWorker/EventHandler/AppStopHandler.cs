using AeFinder.App.Deploy;
using AeFinder.App.Es;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
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

public class AppStopHandler : AppHandlerBase,IDistributedEventHandler<AppStopEto>, ITransientDependency
{
    private readonly IAppDeployManager _kubernetesAppManager;
    private readonly IEntityMappingRepository<AppInfoIndex, string> _appInfoEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionIndex, string> _appSubscriptionEntityMappingRepository;
    private readonly IEntityMappingRepository<AppSubscriptionPodIndex, string> _appSubscriptionPodEntityMappingRepository;

    public AppStopHandler(IAppDeployManager kubernetesAppManager,
        IEntityMappingRepository<AppInfoIndex, string> appInfoEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionIndex, string> appSubscriptionEntityMappingRepository,
        IEntityMappingRepository<AppSubscriptionPodIndex, string> appSubscriptionPodEntityMappingRepository)
    {
        _kubernetesAppManager = kubernetesAppManager;
        _appInfoEntityMappingRepository = appInfoEntityMappingRepository;
        _appSubscriptionEntityMappingRepository = appSubscriptionEntityMappingRepository;
        _appSubscriptionPodEntityMappingRepository = appSubscriptionPodEntityMappingRepository;
    }

    public async Task HandleEventAsync(AppStopEto eventData)
    {
        Logger.LogInformation("[AppStopHandler] Start stop appId: {0}, stopVersion: {1}",
            eventData.AppId, eventData.StopVersion);
        
        var appId = eventData.AppId;
        var version = eventData.StopVersion;
        
        //destroy app pod
        await _kubernetesAppManager.DestroyAppAsync(appId, version);

        //clear stopped version grain data
        await ClearStoppedVersionAppDataAsync(appId, version,
            eventData.StopVersionChainIds);
        
        //update app info index of stopped version
        var appInfoIndex = await _appInfoEntityMappingRepository.GetAsync(appId);
        if (appInfoIndex != null && appInfoIndex.Versions!=null)
        {
            if (appInfoIndex.Versions.CurrentVersion == version)
            {
                appInfoIndex.Versions.CurrentVersion = String.Empty;
            }

            if (appInfoIndex.Versions.PendingVersion == version)
            {
                appInfoIndex.Versions.PendingVersion = String.Empty;
            }

            await _appInfoEntityMappingRepository.AddOrUpdateAsync(appInfoIndex);
        }
        
        //clear app subscription index of stopped version
        var appSubscriptionIndex = await _appSubscriptionEntityMappingRepository.GetAsync(version);
        if (appSubscriptionIndex != null)
        {
            await _appSubscriptionEntityMappingRepository.DeleteAsync(appSubscriptionIndex.Id);
        }
        
        //clear app pod index
        var appSubscriptionPodIndex = await _appSubscriptionPodEntityMappingRepository.GetAsync(version);
        if (appSubscriptionIndex != null)
        {
            await _appSubscriptionPodEntityMappingRepository.DeleteAsync(appSubscriptionPodIndex.Id);
        }
    }

}