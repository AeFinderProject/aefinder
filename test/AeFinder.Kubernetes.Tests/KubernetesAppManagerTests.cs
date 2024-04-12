using AeFinder.Kubernetes.Adapter;
using AeFinder.Kubernetes.Manager;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace AeFinder.Kubernetes.Tests;

public class KubernetesAppManagerTests:AeFinderKubernetesTestBase
{
    private readonly ILogger<KubernetesAppManager> _logger;
    private readonly KubernetesAppManager _appManager;
    private readonly KubernetesOptions _kubernetesOptions;

    public KubernetesAppManagerTests()
    {
        _logger = GetRequiredService<ILogger<KubernetesAppManager>>();
        _appManager = GetRequiredService<KubernetesAppManager>();
    }

    [Fact]
    public async Task KubernetesAppManagerTest_CreateNewApp()
    {
        string appId = "test-app";
        string version = "403dddd97d204b04953faab9ac18fa5e";
        string imageName = "test-image";
        string namespaceName = KubernetesConstants.AppNameSpace;

        string graphqlUrl = await _appManager.CreateNewAppPodAsync(appId, version, imageName);
        graphqlUrl.ShouldBe("www.syb.com/test-app/403dddd97d204b04953faab9ac18fa5e/graphql");
    }
    
    [Fact]
    public async Task KubernetesAppManagerTest_DestroyApp()
    {
        string appId = "test-app";
        string version = "403dddd97d204b04953faab9ac18fa5e";
        string namespaceName = KubernetesConstants.AppNameSpace;

        await _appManager.DestroyAppPodAsync(appId, version);
    }

    private async Task MockKubernetes()
    {
        string appId = "test-app";
        string version = "1.0.0";
        string imageName = "test-image";
        string configMapName = "app-settings-configmap";
        string sideCarConfigName = "filebeat-configmap";
        string deploymentName = "app-deployment";
        string namespaceName = KubernetesConstants.AppNameSpace;
        
        var kubernetesMock = new Moq.Mock<k8s.Kubernetes>(
                new object[] { new KubernetesClientConfiguration() { Host = "http://localhost" } })
            { CallBase = true };

        kubernetesMock.Setup(m => m.ListNamespacedConfigMapAsync(namespaceName,
                It.IsAny<bool?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool?>(),
                It.IsAny<int?>(),
                It.IsAny<bool?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()
            ))
            .ReturnsAsync(new V1ConfigMapList { Items = new List<V1ConfigMap>() });
        
        kubernetesMock.Setup(k => k.ListNamespacedDeploymentAsync(namespaceName,
            It.IsAny<bool?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool?>(),
            It.IsAny<int?>(),
            It.IsAny<bool?>(),
            It.IsAny<bool?>(),
            It.IsAny<CancellationToken>()
        )).ReturnsAsync(new V1DeploymentList { Items = new List<V1Deployment>() });
        
    }
}