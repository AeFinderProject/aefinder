using k8s;
using k8s.Models;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Kubernetes.Adapter;

public class KubernetesClientAdapter : IKubernetesClientAdapter, ISingletonDependency
{
    private readonly k8s.Kubernetes _k8sClient;

    public KubernetesClientAdapter(k8s.Kubernetes k8sClient)
    {
        _k8sClient = k8sClient;
    }
    
    public async Task<V1NamespaceList> ListNamespaceAsync()
    {
        // Call the extension method of the k8s.Kubernetes
        var namespaces =
            await _k8sClient.ListNamespaceAsync();
        return namespaces;
    }

    public async Task<V1Namespace> ReadNamespaceAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        var namespaces =
            await _k8sClient.ReadNamespaceAsync(namespaceParameter, cancellationToken: cancellationToken);
        return namespaces;
    }

    public async Task<V1ConfigMapList> ListConfigMapAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        // Call the extension method of the k8s.Kubernetes
        var configMaps =
            await _k8sClient.ListNamespacedConfigMapAsync(namespaceParameter, cancellationToken: cancellationToken);
        return configMaps;
    }

    public async Task<V1DeploymentList> ListDeploymentAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        // Call the extension method of the k8s.Kubernetes
        var deployments =
            await _k8sClient.ListNamespacedDeploymentAsync(namespaceParameter, cancellationToken: cancellationToken);
        return deployments;
    }

    public async Task<V1ServiceList> ListServiceAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        // Call the extension method of the k8s.Kubernetes
        var services =
            await _k8sClient.ListNamespacedServiceAsync(namespaceParameter, cancellationToken: cancellationToken);
        return services;
    }

    public async Task<V1IngressList> ListIngressAsync(string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        // Call the extension method of the k8s.Kubernetes
        var ingresses =
            await _k8sClient.ListNamespacedIngressAsync(namespaceParameter, cancellationToken: cancellationToken);
        return ingresses;
    }

    public async Task<V1Namespace> CreateNamespaceAsync(V1Namespace nameSpace,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.CreateNamespaceAsync(nameSpace,
            cancellationToken: cancellationToken);
    }
    
    public async Task<V1ConfigMap> CreateConfigMapAsync(V1ConfigMap configMap, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.CreateNamespacedConfigMapAsync(configMap, namespaceParameter,
            cancellationToken: cancellationToken);
    }
    
    public async Task<V1Deployment> CreateDeploymentAsync(V1Deployment deployment, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.CreateNamespacedDeploymentAsync(deployment, namespaceParameter,
            cancellationToken: cancellationToken);
    }
    
    public async Task<V1Service> CreateServiceAsync(V1Service service, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.CreateNamespacedServiceAsync(service, namespaceParameter,
            cancellationToken: cancellationToken);
    }
    
    public async Task<V1Ingress> CreateIngressAsync(V1Ingress ingress, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.CreateNamespacedIngressAsync(ingress, namespaceParameter,
            cancellationToken: cancellationToken);
    }

    public async Task<V1Status> DeleteConfigMapAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.DeleteNamespacedConfigMapAsync(name, namespaceParameter,
            cancellationToken: cancellationToken);
    }
    
    public async Task<V1Status> DeleteDeploymentAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.DeleteNamespacedDeploymentAsync(name, namespaceParameter,
            cancellationToken: cancellationToken);
    }
    
    public async Task<V1Service> DeleteServiceAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.DeleteNamespacedServiceAsync(name, namespaceParameter,
            cancellationToken: cancellationToken);
    }
    
    public async Task<V1Status> DeleteIngressAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.DeleteNamespacedIngressAsync(name, namespaceParameter,
            cancellationToken: cancellationToken);
    }

    public async Task<V1Deployment> ReadNamespacedDeploymentAsync(string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.ReadNamespacedDeploymentAsync(name, namespaceParameter,
            cancellationToken: cancellationToken);
    }

    public async Task<V1Deployment> ReplaceNamespacedDeploymentAsync(V1Deployment deployment,string name, string namespaceParameter,
        CancellationToken cancellationToken = default(CancellationToken))
    {
        return await _k8sClient.ReplaceNamespacedDeploymentAsync(deployment, name, namespaceParameter,
            cancellationToken: cancellationToken);
    }
}