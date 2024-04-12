using AeFinder.Kubernetes.Adapter;
using AeFinder.Kubernetes.Manager;
using k8s;
using k8s.Models;
using Moq;
using Volo.Abp.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;

namespace AeFinder.Kubernetes.Tests;

[DependsOn(typeof(AeFinderKubernetesModule),
    typeof(AbpTestBaseModule))]
public class AeFinderKubernetesTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<KubernetesOptions>(o =>
        {
            o.KubeConfigPath = "/";
            o.AppPodReplicas = 2;
            o.HostName = "www.syb.com";
        });
        var kubernetesMock = new Moq.Mock<k8s.Kubernetes>(
                new object[] { new KubernetesClientConfiguration() { Host = "http://localhost" } })
            { CallBase = true };
        context.Services.AddSingleton(provider => kubernetesMock.Object);
        var mockAdapter = new Mock<IKubernetesClientAdapter>();
        mockAdapter.Setup(m => m.ListConfigMapAsync(KubernetesConstants.AppNameSpace,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new V1ConfigMapList { Items = new List<V1ConfigMap>() });
        mockAdapter.Setup(m => m.ListDeploymentAsync(KubernetesConstants.AppNameSpace,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new V1DeploymentList() { Items = new List<V1Deployment>() });
        mockAdapter.Setup(m => m.ListServiceAsync(KubernetesConstants.AppNameSpace,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new V1ServiceList() { Items = new List<V1Service>() });
        mockAdapter.Setup(m => m.ListIngressAsync(KubernetesConstants.AppNameSpace,
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new V1IngressList() { Items = new List<V1Ingress>() });
        context.Services.AddSingleton(provider => mockAdapter.Object);
        context.Services.AddSingleton<IKubernetesAppManager>(sp => sp.GetService<KubernetesAppManager>());
    }
}