using AeFinder.Metrics.Dto;

namespace AeFinder.Kubernetes.Adapter;

public interface IPrometheusClient
{
    Task<PrometheusResultDto<List<PrometheusContainerUsageDto>>> GetPodContainerCpuUsageInfoAsync(List<string> podNames);
    Task<PrometheusResultDto<List<PrometheusContainerUsageDto>>> GetPodContainerMemoryUsageInfoAsync(List<string> podNames);
}