using k8s.Models;

namespace AeFinder.Kubernetes.ResourceDefinition;

public class IngressHelper
{
    public static string GetAppIngressName(string appId, string version)
    {
        appId = appId.Replace("_", "-");
        return $"ingress-{appId}-{version}".ToLower();
    }
    
    public static V1Ingress CreateAppIngressDefinition(string ingressName, 
        string hostName, string rulePath, string serviceName, int port = 80)
    {
        // Define an Ingress resource for the application
        var ingress = new V1Ingress
        {
            Metadata = new V1ObjectMeta
            {
                Name = ingressName,
                NamespaceProperty = KubernetesConstants.AppNameSpace,
                Annotations = new Dictionary<string, string>
                {
                    // { "nginx.ingress.kubernetes.io/rewrite-target", "/" }
                }
            },
            Spec = new V1IngressSpec
            {
                IngressClassName = KubernetesConstants.NginxIngressClassName, // Set to the name of your IngressClass.
                Rules = new List<V1IngressRule>
                {
                    new V1IngressRule
                    {
                        Host = hostName,
                        Http = new V1HTTPIngressRuleValue
                        {
                            Paths = new List<V1HTTPIngressPath>
                            {
                                new V1HTTPIngressPath
                                {
                                    Path = rulePath,
                                    PathType = "Prefix",
                                    Backend = new V1IngressBackend
                                    {
                                        Service = new V1IngressServiceBackend
                                        {
                                            Name = serviceName,
                                            Port = new V1ServiceBackendPort
                                            {
                                                Number = port
                                            }
                                        }
                                    }
                                },
                                new V1HTTPIngressPath()
                                {
                                    Path = KubernetesConstants.MetricsPath,
                                    PathType = "Exact",
                                    Backend = new V1IngressBackend()
                                    {
                                        Service = new V1IngressServiceBackend()
                                        {
                                            Name = serviceName,
                                            Port = new V1ServiceBackendPort
                                            {
                                                Number = port
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        return ingress;
    }
}