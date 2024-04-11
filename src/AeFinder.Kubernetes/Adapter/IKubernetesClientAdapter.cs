using k8s.Models;

namespace AeFinder.Kubernetes.Adapter;

public interface IKubernetesClientAdapter
{
    Task<V1NamespaceList> ListNamespaceAsync();
    
    Task<V1ConfigMapList> ListConfigMapAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1DeploymentList> ListDeploymentAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1ServiceList> ListServiceAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1IngressList> ListIngressAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Namespace> CreateNamespaceAsync(V1Namespace nameSpace,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1ConfigMap> CreateConfigMapAsync(V1ConfigMap configMap, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Deployment> CreateDeploymentAsync(V1Deployment deployment, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Service> CreateServiceAsync(V1Service service, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Ingress> CreateIngressAsync(V1Ingress ingress, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));

    Task<V1Status> DeleteConfigMapAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));
    
    Task<V1Status> DeleteDeploymentAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));
    
    Task<V1Service> DeleteServiceAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));
    
    Task<V1Status> DeleteIngressAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken));
}