using k8s.Models;

namespace AeFinder.Kubernetes.ResourceDefinition;

public class ServiceMonitorHelper
{
    public static string GetAppServiceMonitorName(string appId, string version)
    {
        appId = appId.Replace("_", "-");
        return $"service-monitor-{appId}-{version}".ToLower();
    }

    public static ServiceMonitor CreateAppServiceMonitorDefinition(string serviceMonitorName, string deploymentName,
        string deploymentLabelName, string servicePortName)
    {
        var serviceMonitor = new ServiceMonitor
        {
            Metadata = new V1ObjectMeta
            {
                Name = serviceMonitorName,
                Labels = new Dictionary<string, string>
                {
                    { KubernetesConstants.AppLabelKey, deploymentLabelName },
                    { "release", "prometheus" }
                },
                NamespaceProperty = KubernetesConstants.AppNameSpace
            },
            Spec = new ServiceMonitorSpec
            {
                Endpoints = new List<Endpoint>
                {
                    new Endpoint
                    {
                        Port = servicePortName,
                        Interval = "15s",
                        Path = KubernetesConstants.MetricsPath
                    }
                },
                NamespaceSelector = new NamespaceSelector
                {
                    MatchNames = new List<string> { KubernetesConstants.AppNameSpace }
                },
                JobLabel = deploymentName,
                Selector = new Selector
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        { KubernetesConstants.AppLabelKey, deploymentLabelName }
                    }
                }
            }
        };

        return serviceMonitor;
    }
}