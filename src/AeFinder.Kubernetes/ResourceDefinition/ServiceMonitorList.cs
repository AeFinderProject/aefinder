namespace AeFinder.Kubernetes.ResourceDefinition;

public class ServiceMonitorList
{
    public string ApiVersion { get; set; }
    public List<ServiceMonitor> Items { get; set; }
}