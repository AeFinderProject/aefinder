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

public class AppUpgradeHandler : IDistributedEventHandler<AppUpgradeEto>, ITransientDependency
{
    private readonly ILogger<AppUpgradeHandler> _logger;
    private readonly IAppDeployManager _kubernetesAppManager;
    private readonly IClusterClient _clusterClient;

    public AppUpgradeHandler(ILogger<AppUpgradeHandler> logger, IAppDeployManager kubernetesAppManager,
        IClusterClient clusterClient)
    {
        _logger = logger;
        _kubernetesAppManager = kubernetesAppManager;
        _clusterClient = clusterClient;
    }

    public async Task HandleEventAsync(AppUpgradeEto eventData)
    {
        _logger.LogInformation("[AppUpgradeHandler] Start upgrade appId: {0}, pendingVersion: {1} currentVersion: {2}",
            eventData.AppId, eventData.PendingVersion, eventData.CurrentVersion);
        
        var appId = eventData.AppId;
        var historyVersion = eventData.CurrentVersion;
        
        await _kubernetesAppManager.DestroyAppAsync(eventData.AppId, eventData.CurrentVersion);

        await HandlerHelper.ClearStoppedVersionAppDataAsync(_clusterClient, appId, historyVersion,
            eventData.CurrentVersionChainIds, _logger);
    }
}