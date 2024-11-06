namespace AeFinder.Kubernetes.Adapter;

public interface IPrometheusClient
{
    Task<string> GetAppPodsResourceInfoAsync(List<string> podNames);
}