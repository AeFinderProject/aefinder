using k8s.Models;

namespace AeFinder.Kubernetes.ResourceDefinition;

public class ServiceHelper
{

    public static string GetAppServiceName(string appId, string version)
    {
        return $"service-{appId}-{version}".ToLower();
    }
    
    public static V1Service CreateAppClusterIPServiceDefinition(string serviceName, 
        string deploymentLabelName,int targetPort, int port = 80)
    {
        var service = new V1Service
        {
            Metadata = new V1ObjectMeta
            {
                Name = serviceName,
                NamespaceProperty = KubernetesConstants.AppNameSpace
            },
            Spec = new V1ServiceSpec
            {
                Selector = new Dictionary<string, string>
                {
                    { KubernetesConstants.AppLabelKey, deploymentLabelName }
                },
                Ports = new List<V1ServicePort>
                {
                    new V1ServicePort
                    {
                        Protocol = "TCP",
                        Port = port,
                        TargetPort = targetPort,
                    }
                },
                Type = "ClusterIP"
            }
        };

        return service;
    }
}