using k8s.Models;

namespace AeFinder.Kubernetes.ResourceDefinition;

public class ServiceMonitorHelper
{
    public static string GetAppServiceMonitorName(string appId, string version)
    {
        appId = appId.Replace("_", "-");
        return $"service-monitor-{appId}-{version}".ToLower();
    }

    public static string GetAppServiceMonitorLabelName(string version)
    {
        return $"service-monitor-{version}".ToLower();
    }
    
    public static ServiceMonitor CreateAppServiceMonitorDefinition(string serviceMonitorName,string serviceMonitorLabelName, 
        string deploymentName, string serviceLabelName, string servicePortName, string metricsPath)
    {
        var serviceMonitor = new ServiceMonitor
        {
            Metadata = new V1ObjectMeta
            {
                Name = serviceMonitorName,
                Labels = new Dictionary<string, string>
                {
                    { KubernetesConstants.AppLabelKey, serviceMonitorLabelName },
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
                        Path = metricsPath
                    }
                },
                NamespaceSelector = new NamespaceSelector
                {
                    MatchNames = new List<string> { KubernetesConstants.AppNameSpace }
                },
                JobLabel = KubernetesConstants.AppLabelKey,
                Selector = new Selector
                {
                    MatchLabels = new Dictionary<string, string>
                    {
                        { KubernetesConstants.AppLabelKey, serviceLabelName }
                    }
                }
            }
        };

        return serviceMonitor;
    }
}