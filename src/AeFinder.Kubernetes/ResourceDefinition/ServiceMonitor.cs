using k8s.Models;

namespace AeFinder.Kubernetes.ResourceDefinition;

public class ServiceMonitor
{
    public string ApiVersion { get; set; } = "monitoring.coreos.com/v1";
    public string Kind { get; set; } = "ServiceMonitor";
    public V1ObjectMeta Metadata { get; set; }
    public ServiceMonitorSpec Spec { get; set; }
}

public class ServiceMonitorSpec
{
    public List<Endpoint> Endpoints { get; set; }
    public NamespaceSelector NamespaceSelector { get; set; }
    public string JobLabel { get; set; }
    public Selector Selector { get; set; }
}

public class Endpoint
{
    public string Port { get; set; }
    public string Interval { get; set; }
    public string Path { get; set; }
}

public class NamespaceSelector
{
    public List<string> MatchNames { get; set; }
}

public class Selector
{
    public Dictionary<string, string> MatchLabels { get; set; }
}