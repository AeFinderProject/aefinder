using AeFinder.App.Es;
using AeFinder.Metrics;
using DnsClient.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class AppPodInfoSyncWorkerBase: AsyncPeriodicBackgroundWorkerBase
{
    protected readonly ILogger<AppPodInfoSyncWorkerBase> Logger;
    protected readonly IKubernetesAppMonitor KubernetesAppMonitor;
    
    protected AppPodInfoSyncWorkerBase(AbpAsyncTimer timer,IServiceScopeFactory serviceScopeFactory,
        ILogger<AppPodInfoSyncWorkerBase> logger,IKubernetesAppMonitor kubernetesAppMonitor): base(timer, serviceScopeFactory)
    {
        Logger = logger;
        KubernetesAppMonitor = kubernetesAppMonitor;
    }
    
    protected override Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        return Task.CompletedTask;
    }

    public async Task<List<AppPodInfoIndex>> UpdatePodResourceUsageAsync(List<AppPodInfoIndex> appPodInfoIndexList)
    {
        //Query resource usage info
        var podNames = appPodInfoIndexList.Select(p => p.PodName).ToList();
        var prometheusPodsInfo = await KubernetesAppMonitor.GetAppPodsResourceInfoFromPrometheusAsync(podNames);
        //Update pod resource usage
        foreach (var podInfoIndex in appPodInfoIndexList)
        {
            var prometheusPodInfo = prometheusPodsInfo.FirstOrDefault(p => p.PodName == podInfoIndex.PodName);
            if (prometheusPodInfo == null)
            {
                Logger.LogInformation($"[AppPodInfoSyncWorkerBase]Pod {podInfoIndex.PodName} not found.");
                continue;
            }

            podInfoIndex.UsageTimestamp = prometheusPodInfo.Timestamp;
            podInfoIndex.CpuUsage = prometheusPodInfo.CpuUsage;
            podInfoIndex.MemoryUsage = prometheusPodInfo.MemoryUsage;

            foreach (var containerInfo in podInfoIndex.Containers)
            {
                var prometheusContainerInfo =
                    prometheusPodInfo.Containers.FirstOrDefault(c =>
                        c.ContainerName == containerInfo.ContainerName);
                if (prometheusContainerInfo == null)
                {
                    Logger.LogInformation($"[AppPodInfoSyncWorkerBase]Container {containerInfo.ContainerName} not found.");
                    continue;
                }

                containerInfo.UsageTimestamp = prometheusContainerInfo.Timestamp;
                containerInfo.CpuUsage = prometheusContainerInfo.CpuUsage;
                containerInfo.MemoryUsage = prometheusContainerInfo.MemoryUsage;
            }
        }

        return appPodInfoIndexList;
    }
}