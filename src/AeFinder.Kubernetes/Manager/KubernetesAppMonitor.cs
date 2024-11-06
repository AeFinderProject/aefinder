using AeFinder.Apps.Dto;
using AeFinder.Kubernetes.Adapter;
using AeFinder.Metrics;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Kubernetes.Manager;

public class KubernetesAppMonitor : IKubernetesAppMonitor, ISingletonDependency
{
    private readonly ILogger<KubernetesAppMonitor> _logger;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;

    public KubernetesAppMonitor(ILogger<KubernetesAppMonitor> logger,
        IKubernetesClientAdapter kubernetesClientAdapter)
    {
        _logger = logger;
        _kubernetesClientAdapter = kubernetesClientAdapter;
    }
    
    public async Task<List<AppPodResourceInfoDto>> GetAppAllPodResourcesAsync()
    {
        var result = new List<AppPodResourceInfoDto>();
        var podsMetrics =
            await _kubernetesClientAdapter.GetKubernetesPodsMetricsByNamespaceAsync(KubernetesConstants.AppNameSpace);
        
        foreach (var podMetric in podsMetrics.Items)
        {
            var podInfoDto = new AppPodResourceInfoDto();
            podInfoDto.PodUid = podMetric.Metadata.Uid;
            podInfoDto.PodName = podMetric.Metadata.Name;
            podInfoDto.CurrentTime = podMetric.Timestamp;
            podInfoDto.Containers = new List<PodContainerResourceDto>();
            foreach (var containerMetric in podMetric.Containers)
            {
                var containerResourceDto = new PodContainerResourceDto();
                containerResourceDto.CpuUsage = containerMetric.Usage["cpu"].Value;
                containerResourceDto.MemoryUsage = containerMetric.Usage["memory"].Value;
                podInfoDto.Containers.Add(containerResourceDto);
            }
            result.Add(podInfoDto);
        }

        return result;
    }
}