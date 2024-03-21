using AeFinder.Kubernetes.ResourceDefinition;
using k8s;
using Microsoft.Extensions.Options;

namespace AeFinder.Kubernetes.Manager;

public class KubernetesAppManager:IKubernetesAppManager
{
    private readonly k8s.Kubernetes _k8sClient;
    private readonly KubernetesOptions _kubernetesOptions;
    public KubernetesAppManager(k8s.Kubernetes k8sClient,IOptionsSnapshot<KubernetesOptions> kubernetesOptions)
    {
        _k8sClient = k8sClient;
        _kubernetesOptions = kubernetesOptions.Value;
    }
    
    public async Task<string> CreateNewAppPodAsync(string appId, string version, string imageName)
    {
        await CreateFullClientTypeAppPod(appId, version, imageName);

        return await CreateQueryClientTypeAppPod(appId, version, imageName);
    }

    private async Task CreateFullClientTypeAppPod(string appId, string version, string imageName)
    {
        //Create full app appsetting config map
        string configMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version,KubernetesConstants.AppClientTypeFull);
        string appSettingsContent = File.ReadAllText(KubernetesConstants.AppSettingTemplateFilePath);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderVersion, version);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeFull);
        var configMaps = await _k8sClient.ListNamespacedConfigMapAsync(KubernetesConstants.AppNameSpace);
        bool configMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName);
        if (!configMapExists)
        {
            var configMap =
                ConfigMapHelper.CreateAppSettingConfigMapDefinition(configMapName, appSettingsContent);
            // Submit the ConfigMap to the cluster
            await _k8sClient.CreateNamespacedConfigMapAsync(configMap,KubernetesConstants.AppNameSpace);
        }
        //Create full app filebeat config map
        string sideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId,version,KubernetesConstants.AppClientTypeFull);
        var sideCarConfigContent = File.ReadAllText(KubernetesConstants.AppFileBeatConfigTemplateFilePath);
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeFull);
        bool sideCarConfigMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == sideCarConfigName);
        if (!sideCarConfigMapExists)
        {
            var sideCarConfigMap =
                ConfigMapHelper.CreateFileBeatConfigMapDefinition(sideCarConfigName, sideCarConfigContent);
            // Submit the ConfigMap to the cluster
            await _k8sClient.CreateNamespacedConfigMapAsync(sideCarConfigMap,KubernetesConstants.AppNameSpace);
        }
        //Create full app deployment
        string deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeFull);
        string containerName =
            ContainerHelper.GetAppContainerName(appId, version, KubernetesConstants.AppClientTypeFull);
        int replicasCount = _kubernetesOptions.AppPodReplicas;
        var deployments = await _k8sClient.ListNamespacedDeploymentAsync(KubernetesConstants.AppNameSpace);
        bool deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (!deploymentExists)
        {
            var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(imageName,
                deploymentName, replicasCount, containerName, configMapName, sideCarConfigName);
            // Create Deployment
            await _k8sClient.CreateNamespacedDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
        }
    }

    private async Task<string> CreateQueryClientTypeAppPod(string appId, string version, string imageName)
    {
        //Create query app appsetting config map
        string configMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version,KubernetesConstants.AppClientTypeQuery);
        string appSettingsContent = File.ReadAllText(KubernetesConstants.AppSettingTemplateFilePath);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderVersion, version);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeQuery);
        var configMaps = await _k8sClient.ListNamespacedConfigMapAsync(KubernetesConstants.AppNameSpace);
        bool configMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName);
        if (!configMapExists)
        {
            var configMap =
                ConfigMapHelper.CreateAppSettingConfigMapDefinition(configMapName, appSettingsContent);
            // Submit the ConfigMap to the cluster
            await _k8sClient.CreateNamespacedConfigMapAsync(configMap,KubernetesConstants.AppNameSpace);
        }
        
        //Create query app filebeat config map
        string sideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId,version,KubernetesConstants.AppClientTypeQuery);
        var sideCarConfigContent = File.ReadAllText(KubernetesConstants.AppFileBeatConfigTemplateFilePath);
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeQuery);
        bool sideCarConfigMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == sideCarConfigName);
        if (!sideCarConfigMapExists)
        {
            var sideCarConfigMap =
                ConfigMapHelper.CreateFileBeatConfigMapDefinition(sideCarConfigName, sideCarConfigContent);
            // Submit the ConfigMap to the cluster
            await _k8sClient.CreateNamespacedConfigMapAsync(sideCarConfigMap,KubernetesConstants.AppNameSpace);
        }
        
        //Create query app deployment
        string deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery);
        string containerName =
            ContainerHelper.GetAppContainerName(appId, version, KubernetesConstants.AppClientTypeQuery);
        int replicasCount = _kubernetesOptions.AppPodReplicas;
        var deployments = await _k8sClient.ListNamespacedDeploymentAsync(KubernetesConstants.AppNameSpace);
        bool deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (!deploymentExists)
        {
            var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(imageName,
                deploymentName, replicasCount, containerName, configMapName, sideCarConfigName);
            // Create Deployment
            await _k8sClient.CreateNamespacedDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
        }
        
        //Create query app service
        string serviceName = ServiceHelper.GetAppServiceName(appId, version);
        int targetPort = KubernetesConstants.AppContainerTargetPort;
        var services = await _k8sClient.ListNamespacedServiceAsync(KubernetesConstants.AppNameSpace);
        bool sericeExists = services.Items.Any(item => item.Metadata.Name == serviceName);
        if (!sericeExists)
        {
            var service =
                ServiceHelper.CreateAppClusterIPServiceDefinition(serviceName, deploymentName, targetPort);
            // Create Service
            await _k8sClient.CreateNamespacedServiceAsync(service, KubernetesConstants.AppNameSpace);
        }
        
        //Create query app ingress
        string ingressName = IngressHelper.GetAppIngressName(appId, version);
        string hostName = _kubernetesOptions.HostName;
        // string rulePath = $"/{appId}";
        string rulePath = $"/{appId}/{version}/graphql";
        var ingresses = await _k8sClient.ListNamespacedIngressAsync(KubernetesConstants.AppNameSpace);
        bool ingressExists = ingresses.Items.Any(item => item.Metadata.Name == ingressName);
        if (!ingressExists)
        {
            var ingress =
                IngressHelper.CreateAppIngressDefinition(ingressName, hostName,
                    rulePath, serviceName, targetPort);
            // Submit the Ingress to the cluster
            await _k8sClient.CreateNamespacedIngressAsync(ingress, KubernetesConstants.AppNameSpace); 
        }

        return hostName + rulePath;
    }
    
    public async Task DestroyAppPodAsync(string appId, string version)
    {
        //Delete full app deployment
        
        //Delete full app appsetting config map
        
        //Delete full app filebeat config map
        
        
        //Delete query app deployment

        //Delete query app appsetting config map
        
        //Delete query app filebeat config map
        
        //Delete query app service
        
        //Delete query app ingress
    }
}