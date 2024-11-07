using AeFinder.Apps.Dto;
using AeFinder.Kubernetes.Adapter;
using AeFinder.Metrics;
using AeFinder.Metrics.Dto;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Kubernetes.Manager;

public class KubernetesAppMonitor : IKubernetesAppMonitor, ISingletonDependency
{
    private readonly ILogger<KubernetesAppMonitor> _logger;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;
    private readonly IPrometheusClient _prometheusClient;

    public KubernetesAppMonitor(ILogger<KubernetesAppMonitor> logger,
        IPrometheusClient prometheusClient,
        IKubernetesClientAdapter kubernetesClientAdapter)
    {
        _logger = logger;
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _prometheusClient = prometheusClient;
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
                containerResourceDto.CpuUsage = containerMetric.Usage["cpu"].ToString();
                containerResourceDto.MemoryUsage = containerMetric.Usage["memory"].ToString();
                podInfoDto.Containers.Add(containerResourceDto);
            }
            result.Add(podInfoDto);
        }

        return result;
    }

    public async Task<List<AppPodResourceInfoDto>> GetAppPodsResourceInfoFromPrometheusAsync(List<string> podsName)
    {
        var cpuUsage = await _prometheusClient.GetPodContainerCpuUsageInfoAsync(podsName);
        var cpuUsageDto = JsonConvert.DeserializeObject<PrometheusContainerCpuUsageDto>(cpuUsage);
        
        var memoryUsage = await _prometheusClient.GetPodContainerMemoryUsageInfoAsync(podsName);
        var memoryUsageDto=JsonConvert.DeserializeObject<PrometheusContainerMemoryUsageDto>(memoryUsage);

        var result = new List<AppPodResourceInfoDto>();


        return result;
    }
    
    
}