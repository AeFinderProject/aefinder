using AeFinder.Kubernetes.Manager;
using AeFinder.Studio.Eto;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppUpgradeHandler : IDistributedEventHandler<AppUpgradeEto>, ITransientDependency
{
    private readonly ILogger<AppUpgradeHandler> _logger;
    private readonly IKubernetesAppManager _kubernetesAppManager;

    public AppUpgradeHandler(ILogger<AppUpgradeHandler> logger, IKubernetesAppManager kubernetesAppManager)
    {
        _logger = logger;
        _kubernetesAppManager = kubernetesAppManager;
    }

    public async Task HandleEventAsync(AppUpgradeEto eventData)
    {
        await _kubernetesAppManager.DestroyAppPodAsync(eventData.AppId, eventData.CurrentVersion);
        _logger.LogInformation("destroy app pod, appId: {0}, newVersion: {1} currentVersion: {2}", eventData.AppId, eventData.NewVersion, eventData.CurrentVersion);
    }
}