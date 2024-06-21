using System.Text.Json.Serialization;

namespace AeFinder.Kubernetes.ResourceDefinition;

public class ServiceMonitorList
{
    [JsonPropertyName("apiVersion")]
    public string ApiVersion { get; set; }
    [JsonPropertyName("items")]
    public List<ServiceMonitor> Items { get; set; }
}