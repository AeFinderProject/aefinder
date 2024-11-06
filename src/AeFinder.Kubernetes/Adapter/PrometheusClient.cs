using Volo.Abp.DependencyInjection;

namespace AeFinder.Kubernetes.Adapter;

public class PrometheusClient: IPrometheusClient, ISingletonDependency
{
    private readonly string prometheusBaseUrl = "http://your-prometheus-server:9090";
    
    public PrometheusClient()
    {
    }
    
    public async Task<string> GetAppPodsResourceInfoAsync(List<string> podNames)
    {
        string prometheusBaseUrl = "http://your-prometheus-server:9090";
        var query = $"sum(rate(container_cpu_usage_seconds_total{{namespace='{KubernetesConstants.AppNameSpace}', pod=~'{string.Join("|", podNames)}'}}[5m])) by (pod)";
        string result = await QueryPrometheusAsync(prometheusBaseUrl, query);
        return result;
    }
    
    private async Task<string> QueryPrometheusAsync(string prometheusBaseUrl, string query)
    {
        using (var httpClient = new HttpClient())
        {
            var uri = $"{prometheusBaseUrl}/api/v1/query" +
                      $"?query={Uri.EscapeDataString(query)}";

            var response = await httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
    }
}