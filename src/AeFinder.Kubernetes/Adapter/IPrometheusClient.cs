namespace AeFinder.Kubernetes.Adapter;

public interface IPrometheusClient
{
    Task<string> GetPodContainerCpuUsageInfoAsync(List<string> podNames);
    Task<string> GetPodContainerMemoryUsageInfoAsync(List<string> podNames);
}