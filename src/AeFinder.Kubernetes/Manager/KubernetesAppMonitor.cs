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
            podInfoDto.PodName = podMetric.Metadata.Name;
            podInfoDto.Timestamp = podMetric.Timestamp == null ? 0 : ToUnixTimeMilliseconds(podMetric.Timestamp.Value);
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
        // _logger.LogInformation($"cpuUsage: {cpuUsage}");
        var cpuUsageResultDto = JsonConvert.DeserializeObject<PrometheusResultDto<List<PrometheusContainerUsageDto>>>(cpuUsage);
        
        var memoryUsage = await _prometheusClient.GetPodContainerMemoryUsageInfoAsync(podsName);
        // _logger.LogInformation($"memoryUsage: {memoryUsage}");
        var memoryUsageResultDto=JsonConvert.DeserializeObject<PrometheusResultDto<List<PrometheusContainerUsageDto>>>(memoryUsage);

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
                    if (cpuUsageDto.Metric.Container == KubernetesConstants.FileBeatContainerName)
                    {
                        var fileBeatContainerInfo = podInfo.Containers.FirstOrDefault(c =>
                            c.ContainerName == KubernetesConstants.FileBeatContainerName);
                        if (fileBeatContainerInfo == null)
                        {
                            fileBeatContainerInfo = new PodContainerResourceDto()
                            {
                                ContainerName = cpuUsageDto.Metric.Container,
                                Timestamp = Convert.ToInt64(cpuUsageDto.Value[0]),
                                CpuUsage = cpuUsageDto.Value[1].ToString()
                            };
                            podInfo.Containers.Add(fileBeatContainerInfo);
                        }
                        else
                        {
                            fileBeatContainerInfo.Timestamp = Convert.ToInt64(cpuUsageDto.Value[0]);
                            fileBeatContainerInfo.CpuUsage = cpuUsageDto.Value[1].ToString();
                        }
                    }
                    else
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
                    if (memoryUsageDto.Metric.Container == KubernetesConstants.FileBeatContainerName)
                    {
                        var fileBeatContainerInfo = podInfo.Containers.FirstOrDefault(c =>
                            c.ContainerName == KubernetesConstants.FileBeatContainerName);
                        if (fileBeatContainerInfo == null)
                        {
                            fileBeatContainerInfo = new PodContainerResourceDto()
                            {
                                ContainerName = memoryUsageDto.Metric.Container,
                                Timestamp = Convert.ToInt64(memoryUsageDto.Value[0]),
                                MemoryUsage = memoryUsageDto.Value[1].ToString()
                            };
                            podInfo.Containers.Add(fileBeatContainerInfo);
                        }
                        else
                        {
                            fileBeatContainerInfo.Timestamp = Convert.ToInt64(memoryUsageDto.Value[0]);
                            fileBeatContainerInfo.MemoryUsage = memoryUsageDto.Value[1].ToString();
                        }
                    }
                    else
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
            }
        }

        return result;
    }
    
    private static long ToUnixTimeMilliseconds(DateTime value)
    {
        var span = value - DateTime.UnixEpoch;
        return (long) span.TotalMilliseconds;
    }
}