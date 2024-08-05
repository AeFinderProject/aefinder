using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Apps.Eto;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppStopHandler : IDistributedEventHandler<AppStopEto>, ITransientDependency
{
    private readonly ILogger<AppStopHandler> _logger;
    private readonly IAppDeployManager _kubernetesAppManager;
    private readonly IClusterClient _clusterClient;

    public AppStopHandler(ILogger<AppStopHandler> logger, IAppDeployManager kubernetesAppManager,
        IClusterClient clusterClient)
    {
        _logger = logger;
        _kubernetesAppManager = kubernetesAppManager;
        _clusterClient = clusterClient;
    }

    public async Task HandleEventAsync(AppStopEto eventData)
    {
        var appId = eventData.AppId;
        var version = eventData.StopVersion;
        
        await _kubernetesAppManager.DestroyAppAsync(appId, version);

        await HandlerHelper.ClearStoppedVersionAppDataAsync(_clusterClient, appId, version,
            eventData.StopVersionChainIds, _logger);
    }

    

}