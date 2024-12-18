using AeFinder.Metrics.Dto;
using AeFinder.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Kubernetes.Adapter;

public class PrometheusClient: IPrometheusClient, ISingletonDependency
{
    private readonly KubernetesOptions _kubernetesOptions;
    private readonly IHttpClientFactory _httpClientFactory;

    public PrometheusClient(IOptionsSnapshot<KubernetesOptions> kubernetesOptions, IHttpClientFactory httpClientFactory)
    {
        _kubernetesOptions = kubernetesOptions.Value;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<PrometheusResultDto<List<PrometheusContainerUsageDto>>> GetPodContainerCpuUsageInfoAsync(List<string> podNames)
    {
        // var query = $"sum(rate(container_cpu_usage_seconds_total{{namespace='{KubernetesConstants.AppNameSpace}', pod=~'{string.Join("|", podNames)}'}}[5m])) by (pod)";
        var query = $"sum(rate(container_cpu_usage_seconds_total{{namespace='{KubernetesConstants.AppNameSpace}', pod=~'{string.Join("|", podNames)}', container!='POD'}}[5m])) by (pod, container)";
        string result = await QueryPrometheusAsync(query);
        var resultDto = JsonConvert.DeserializeObject<PrometheusResultDto<List<PrometheusContainerUsageDto>>>(result);
        return resultDto;
    }
    
    public async Task<PrometheusResultDto<List<PrometheusContainerUsageDto>>> GetPodContainerMemoryUsageInfoAsync(List<string> podNames)
    {
        // var query = $"sum(container_memory_usage_bytes{{namespace='{KubernetesConstants.AppNameSpace}', pod=~'{string.Join("|", podNames)}'}}) by (pod)";
        var query = $"sum(container_memory_usage_bytes{{namespace='{KubernetesConstants.AppNameSpace}', pod=~'{string.Join("|", podNames)}', container!='POD'}}) by (pod, container)";
        string result = await QueryPrometheusAsync(query);
        var resultDto = JsonConvert.DeserializeObject<PrometheusResultDto<List<PrometheusContainerUsageDto>>>(result);
        return resultDto;
    }

    private async Task<string> QueryPrometheusAsync(string query)
    {
        string prometheusBaseUrl = _kubernetesOptions.PrometheusUrl;
        var httpClient = _httpClientFactory.CreateClient();
        var uri = $"{prometheusBaseUrl}/api/v1/query" +
                  $"?query={Uri.EscapeDataString(query)}";

        var response = await httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}