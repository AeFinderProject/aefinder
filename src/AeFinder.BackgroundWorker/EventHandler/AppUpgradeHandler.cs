using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppUpgradeHandler : AppHandlerBase,IDistributedEventHandler<AppUpgradeEto>, ITransientDependency
{
    private readonly IAppDeployManager _kubernetesAppManager;

    public AppUpgradeHandler(IAppDeployManager kubernetesAppManager)
    {
        _kubernetesAppManager = kubernetesAppManager;
    }

    public async Task HandleEventAsync(AppUpgradeEto eventData)
    {
        Logger.LogInformation("[AppUpgradeHandler] Start upgrade appId: {0}, pendingVersion: {1} currentVersion: {2}",
            eventData.AppId, eventData.PendingVersion, eventData.CurrentVersion);
        
        var appId = eventData.AppId;
        var historyVersion = eventData.CurrentVersion;
        
        await _kubernetesAppManager.DestroyAppAsync(eventData.AppId, eventData.CurrentVersion);

        await ClearStoppedVersionAppDataAsync(appId, historyVersion,
            eventData.CurrentVersionChainIds);
    }
}