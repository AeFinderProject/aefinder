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

public class AppStopHandler : AppHandlerBase,IDistributedEventHandler<AppStopEto>, ITransientDependency
{
    private readonly IAppDeployManager _kubernetesAppManager;

    public AppStopHandler(IAppDeployManager kubernetesAppManager)
    {
        _kubernetesAppManager = kubernetesAppManager;
    }

    public async Task HandleEventAsync(AppStopEto eventData)
    {
        Logger.LogInformation("[AppStopHandler] Start stop appId: {0}, stopVersion: {1}",
            eventData.AppId, eventData.StopVersion);
        
        var appId = eventData.AppId;
        var version = eventData.StopVersion;
        
        await _kubernetesAppManager.DestroyAppAsync(appId, version);

        await ClearStoppedVersionAppDataAsync(appId, version,
            eventData.StopVersionChainIds);
    }

}