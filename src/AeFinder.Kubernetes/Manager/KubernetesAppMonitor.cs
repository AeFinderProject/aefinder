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

    public async Task<List<AppPodResourceInfoDto>> GetAppPodsResourceInfoFromPrometheusAsync(List<string> podsName)
    {
        var cpuUsageResultDto = await _prometheusClient.GetPodContainerCpuUsageInfoAsync(podsName);
        
        var memoryUsageResultDto = await _prometheusClient.GetPodContainerMemoryUsageInfoAsync(podsName);

        var result = new List<AppPodResourceInfoDto>();

        if (cpuUsageResultDto.Status == "success")
        {
            foreach (var cpuUsageDto in cpuUsageResultDto.Data.Result)
            {
                var podInfo = result.FirstOrDefault(p => p.PodName == cpuUsageDto.Metric.Pod);
                if (podInfo == null)
                {
                    podInfo = new AppPodResourceInfoDto()
                    {
                        PodName = cpuUsageDto.Metric.Pod,
                        Timestamp = Convert.ToInt64(cpuUsageDto.Value[0]),
                        Containers = new List<PodContainerResourceDto>()
                    };
                    result.Add(podInfo);
                }
                //pod metric
                if (cpuUsageDto.Metric.Container.IsNullOrEmpty())
                {
                    podInfo.Timestamp = Convert.ToInt64(cpuUsageDto.Value[0]);
                    podInfo.CpuUsage = cpuUsageDto.Value[1].ToString();
                }
                //container metric
                else
                {
                    await SetContainerCpuUsageInfoAsync(podInfo, cpuUsageDto);
                }
            }
        }
        
        if (memoryUsageResultDto.Status == "success")
        {
            foreach (var memoryUsageDto in memoryUsageResultDto.Data.Result)
            {
                var podInfo = result.FirstOrDefault(p => p.PodName == memoryUsageDto.Metric.Pod);
                if (podInfo == null)
                {
                    podInfo = new AppPodResourceInfoDto()
                    {
                        PodName = memoryUsageDto.Metric.Pod,
                        Timestamp = Convert.ToInt64(memoryUsageDto.Value[0]),
                        Containers = new List<PodContainerResourceDto>()
                    };
                    result.Add(podInfo);
                }
                //pod metric
                if (memoryUsageDto.Metric.Container.IsNullOrEmpty())
                {
                    podInfo.Timestamp = Convert.ToInt64(memoryUsageDto.Value[0]);
                    podInfo.MemoryUsage = memoryUsageDto.Value[1].ToString();
                }
                //container metric
                else
                {
                    await SetContainerMemoryUsageInfoAsync(podInfo, memoryUsageDto);
                }
            }
        }

        return result;
    }

    private async Task SetContainerCpuUsageInfoAsync(AppPodResourceInfoDto podInfo, PrometheusContainerUsageDto cpuUsageDto)
    {
        var appContainerInfo = podInfo.Containers.FirstOrDefault(c =>
            c.ContainerName == cpuUsageDto.Metric.Container);
        if (appContainerInfo == null)
        {
            appContainerInfo = new PodContainerResourceDto()
            {
                ContainerName = cpuUsageDto.Metric.Container,
                Timestamp = Convert.ToInt64(cpuUsageDto.Value[0]),
                CpuUsage = cpuUsageDto.Value[1].ToString()
            };
            podInfo.Containers.Add(appContainerInfo);
        }
        else
        {
            appContainerInfo.Timestamp = Convert.ToInt64(cpuUsageDto.Value[0]);
            appContainerInfo.CpuUsage = cpuUsageDto.Value[1].ToString();
        }
    }

    private async Task SetContainerMemoryUsageInfoAsync(AppPodResourceInfoDto podInfo, PrometheusContainerUsageDto memoryUsageDto)
    {
        var appContainerInfo = podInfo.Containers.FirstOrDefault(c =>
            c.ContainerName == memoryUsageDto.Metric.Container);
        if (appContainerInfo == null)
        {
            appContainerInfo = new PodContainerResourceDto()
            {
                ContainerName = memoryUsageDto.Metric.Container,
                Timestamp = Convert.ToInt64(memoryUsageDto.Value[0]),
                MemoryUsage = memoryUsageDto.Value[1].ToString()
            };
            podInfo.Containers.Add(appContainerInfo);
        }
        else
        {
            appContainerInfo.Timestamp = Convert.ToInt64(memoryUsageDto.Value[0]);
            appContainerInfo.MemoryUsage = memoryUsageDto.Value[1].ToString();
        }
    }
}