using AeFinder.App.Deploy;
using AeFinder.Apps;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppUpgradeHandler : IDistributedEventHandler<AppUpgradeEto>, ITransientDependency
{
    private readonly ILogger<AppUpgradeHandler> _logger;
    private readonly IAppDeployManager _kubernetesAppManager;

    public AppUpgradeHandler(ILogger<AppUpgradeHandler> logger, IAppDeployManager kubernetesAppManager)
    {
        _logger = logger;
        _kubernetesAppManager = kubernetesAppManager;
    }

    public async Task HandleEventAsync(AppUpgradeEto eventData)
    {
        await _kubernetesAppManager.DestroyAppAsync(eventData.AppId, eventData.CurrentVersion);
        _logger.LogInformation("destroy app pod, appId: {0}, pendingVersion: {1} currentVersion: {2}", eventData.AppId, eventData.PendingVersion, eventData.CurrentVersion);
    }
}