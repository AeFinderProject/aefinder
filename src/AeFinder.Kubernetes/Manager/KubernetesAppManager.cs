using System.Text.Json;
using AeFinder.App.Deploy;
using AeFinder.Apps.Dto;
using AeFinder.Apps.Eto;
using AeFinder.Kubernetes.Adapter;
using AeFinder.Kubernetes.ResourceDefinition;
using AeFinder.Options;
using AElf.ExceptionHandler;
using k8s;
using k8s.Autorest;
using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.Kubernetes.Manager;

public partial class KubernetesAppManager: IAppDeployManager, ISingletonDependency
{
    // private readonly k8s.Kubernetes _k8sClient;
    private readonly KubernetesOptions _kubernetesOptions;
    private readonly ILogger<KubernetesAppManager> _logger;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;
    private readonly IAppResourceLimitProvider _appResourceLimitProvider;
    private readonly IDistributedEventBus _distributedEventBus;

    public KubernetesAppManager(ILogger<KubernetesAppManager> logger,
        IKubernetesClientAdapter kubernetesClientAdapter,
        IAppResourceLimitProvider appResourceLimitProvider,
        IDistributedEventBus distributedEventBus,
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions)
    {
        _logger = logger;
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _kubernetesOptions = kubernetesOptions.Value;
        _appResourceLimitProvider = appResourceLimitProvider;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<string> CreateNewAppAsync(string appId, string version, string imageName, List<string> chainIds)
    {
        // await CheckNameSpaceAsync();
        if (chainIds.Count == 0)
        {
            await CreateFullClientTypeAppPodAsync(appId, version, imageName, null);
        }
        else
        {
            foreach (var chainId in chainIds)
            {
                await CreateFullClientTypeAppPodAsync(appId, version, imageName, chainId);
            }
        }

        //Publish app pod update eto to background worker
        await _distributedEventBus.PublishAsync(new AppPodUpdateEto()
        {
            AppId = appId,
            Version = version,
            DockerImage = imageName
        });

        return await CreateQueryClientTypeAppPodAsync(appId, version, imageName);
    }

    private async Task CreateFullClientTypeAppPodAsync(string appId, string version, string imageName, string chainId)
    {
        //Create full app appsetting config map
        var configMapName =
            ConfigMapHelper.GetAppSettingConfigMapName(appId, version, KubernetesConstants.AppClientTypeFull, chainId);
        var appSettingsContent = File.ReadAllText(KubernetesConstants.AppSettingTemplateFilePath);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderVersion, version);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeFull);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderChainId, chainId);
        var resourceLimitInfo = await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxEntityCallCount,
            resourceLimitInfo.MaxEntityCallCount.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxEntitySize,
            resourceLimitInfo.MaxEntitySize.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxLogCallCount,
            resourceLimitInfo.MaxLogCallCount.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxLogSize,
            resourceLimitInfo.MaxLogSize.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxContractCallCount,
            resourceLimitInfo.MaxContractCallCount.ToString());
        var clientName = $"AeFinder_App-{appId}-{version}";
        if (!chainId.IsNullOrWhiteSpace())
        {
            clientName += $"-{chainId}";
        }

        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderEventBusClientName,
            clientName);
        var exchangeName = $"AeFinder_App-{version}";
        if (!chainId.IsNullOrWhiteSpace())
        {
            exchangeName += $"-{chainId}";
        }

        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderEventBusExchangeName,
            exchangeName);

        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        var configMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName);
        if (!configMapExists)
        {
            var configMap =
                ConfigMapHelper.CreateAppSettingConfigMapDefinition(configMapName, appSettingsContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(configMap, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {configMapName} created", configMapName);
        }

        //Create full app filebeat config map
        var sideCarConfigName =
            ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version, KubernetesConstants.AppClientTypeFull, chainId);
        var sideCarConfigContent = File.ReadAllText(KubernetesConstants.AppFileBeatConfigTemplateFilePath);
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderAppId, appId.ToLower());
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderVersion, version.ToLower());
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderNameSpace,
            KubernetesConstants.AppNameSpace.ToLower());
        var sideCarConfigMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == sideCarConfigName);
        if (!sideCarConfigMapExists)
        {
            var sideCarConfigMap =
                ConfigMapHelper.CreateFileBeatConfigMapDefinition(sideCarConfigName, sideCarConfigContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(sideCarConfigMap, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {sideCarConfigName} created", sideCarConfigName);
        }

        //Create full app deployment
        var deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeFull, chainId);
        var deploymentLabelName =
            DeploymentHelper.GetAppDeploymentLabelName(version, KubernetesConstants.AppClientTypeFull, chainId);
        var containerName =
            ContainerHelper.GetAppContainerName(appId, version, KubernetesConstants.AppClientTypeFull, chainId);
        var targetPort = KubernetesConstants.AppContainerTargetPort;
        var replicasCount = 1; //Only one pod instance is allowed
        var requestCpuCore = resourceLimitInfo.AppFullPodRequestCpuCore;
        var requestMemory = resourceLimitInfo.AppFullPodRequestMemory;
        var limitCpuCore = resourceLimitInfo.AppFullPodLimitCpuCore;
        var limitMemory = resourceLimitInfo.AppFullPodLimitMemory;
        var maxSurge = KubernetesConstants.FullPodMaxSurge;
        var maxUnavailable = KubernetesConstants.FullPodMaxUnavailable;
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (!deploymentExists)
        {
            var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(appId, version,
                KubernetesConstants.AppClientTypeFull, chainId, imageName, deploymentName, deploymentLabelName,
                replicasCount, containerName, targetPort, configMapName, sideCarConfigName, requestCpuCore, requestMemory,
                limitCpuCore, limitMemory, maxSurge, maxUnavailable);
            // Create Deployment
            await _kubernetesClientAdapter.CreateDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
            _logger.LogInformation(
                "[KubernetesAppManager]Deployment {deploymentName} created, requestCpuCore: {requestCpuCore} requestMemory: {requestMemory} limitCpuCore: {limitCpuCore} limitMemory: {limitMemory}",
                deploymentName, requestCpuCore, requestMemory, limitCpuCore, limitMemory);
        }

        //Set the app resource limit as it deployed
        await _appResourceLimitProvider.SetAppResourceLimitAsync(appId, resourceLimitInfo);
    }

    private async Task<string> CreateQueryClientTypeAppPodAsync(string appId, string version, string imageName)
    {
        //Create query app appsetting config map
        var configMapName =
            ConfigMapHelper.GetAppSettingConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var appSettingsContent = File.ReadAllText(KubernetesConstants.AppSettingTemplateFilePath);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderVersion, version);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeQuery);
        var resourceLimitInfo = await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxEntityCallCount,
            resourceLimitInfo.MaxEntityCallCount.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxEntitySize,
            resourceLimitInfo.MaxEntitySize.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxLogCallCount,
            resourceLimitInfo.MaxLogCallCount.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxLogSize,
            resourceLimitInfo.MaxLogSize.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxContractCallCount,
            resourceLimitInfo.MaxContractCallCount.ToString());
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        var configMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName);
        if (!configMapExists)
        {
            var configMap =
                ConfigMapHelper.CreateAppSettingConfigMapDefinition(configMapName, appSettingsContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(configMap, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {configMapName} created", configMapName);
        }

        //Create query app filebeat config map
        var sideCarConfigName =
            ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var sideCarConfigContent = File.ReadAllText(KubernetesConstants.AppFileBeatConfigTemplateFilePath);
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderAppId, appId.ToLower());
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderVersion, version.ToLower());
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderNameSpace,
            KubernetesConstants.AppNameSpace.ToLower());
        var sideCarConfigMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == sideCarConfigName);
        if (!sideCarConfigMapExists)
        {
            var sideCarConfigMap =
                ConfigMapHelper.CreateFileBeatConfigMapDefinition(sideCarConfigName, sideCarConfigContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(sideCarConfigMap, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {sideCarConfigName} created", sideCarConfigName);
        }

        //Create query app deployment
        var deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var deploymentLabelName =
            DeploymentHelper.GetAppDeploymentLabelName(version, KubernetesConstants.AppClientTypeQuery, null);
        var containerName =
            ContainerHelper.GetAppContainerName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var targetPort = KubernetesConstants.AppContainerTargetPort;
        var replicasCount = resourceLimitInfo.AppPodReplicas;
        var requestCpuCore = resourceLimitInfo.AppQueryPodRequestCpuCore;
        var requestMemory = resourceLimitInfo.AppQueryPodRequestMemory;
        var limitCpuCore = string.Empty;
        var limitMemory = string.Empty;
        var maxSurge = KubernetesConstants.QueryPodMaxSurge;
        var maxUnavailable = KubernetesConstants.QueryPodMaxUnavailable;
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (!deploymentExists)
        { 
            var healthPath = GetGraphQLPlaygroundPath(appId, version);
            var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(appId, version,
                KubernetesConstants.AppClientTypeQuery, string.Empty, imageName, deploymentName, deploymentLabelName,
                replicasCount, containerName, targetPort, configMapName, sideCarConfigName, requestCpuCore, requestMemory, 
                limitCpuCore, limitMemory, maxSurge, maxUnavailable, healthPath);
            // Create Deployment
            await _kubernetesClientAdapter.CreateDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
            _logger.LogInformation(
                "[KubernetesAppManager]Deployment {deploymentName} created, requestCpuCore: {requestCpuCore} requestMemory: {requestMemory}",
                deploymentName, requestCpuCore, requestMemory);
        }

        //Create query app service
        var serviceName = ServiceHelper.GetAppServiceName(appId, version);
        var serviceLabelName = ServiceHelper.GetAppServiceLabelName(appId, version);
        var servicePortName = ServiceHelper.GetAppServicePortName(version);
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        var serviceExists = services.Items.Any(item => item.Metadata.Name == serviceName);
        if (!serviceExists)
        {
            var service =
                ServiceHelper.CreateAppClusterIPServiceDefinition(appId, serviceName, serviceLabelName,
                    deploymentLabelName, servicePortName, targetPort);
            // Create Service
            await _kubernetesClientAdapter.CreateServiceAsync(service, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Service {serviceName} created", serviceName);
        }

        //Create query app ingress
        var ingressName = IngressHelper.GetAppIngressName(appId, version);
        var hostName = _kubernetesOptions.HostName;
        // string rulePath = $"/{appId}";
        var rulePath = $"/{appId}/{version}";
        var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
        var ingressExists = ingresses.Items.Any(item => item.Metadata.Name == ingressName);
        if (!ingressExists)
        {
            var ingress =
                IngressHelper.CreateAppIngressDefinition(ingressName, hostName,
                    rulePath, serviceName, targetPort);
            // Submit the Ingress to the cluster
            await _kubernetesClientAdapter.CreateIngressAsync(ingress, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Ingress {ingressName} created", ingressName);
        }

        //Create app service monitor
        var serviceMonitorName = ServiceMonitorHelper.GetAppServiceMonitorName(appId);
        var serviceMonitorExists = await ExistsServiceMonitorAsync(serviceMonitorName);
        var metricsPath = rulePath + KubernetesConstants.MetricsPath;
        if (!serviceMonitorExists)
        {
            var serviceMonitor = ServiceMonitorHelper.CreateAppServiceMonitorDefinition(appId, serviceMonitorName,
                servicePortName, metricsPath);
            //Create Service Monitor
            await _kubernetesClientAdapter.CreateServiceMonitorAsync(serviceMonitor, KubernetesConstants.MonitorGroup,
                KubernetesConstants.CoreApiVersion, KubernetesConstants.AppNameSpace,
                KubernetesConstants.MonitorPlural);
            _logger.LogInformation("[KubernetesAppManager]ServiceMonitor {serviceMonitorName} created",
                serviceMonitorName);
        }

        return hostName + rulePath + "/graphql";
    }

    private string GetGraphQLPath(string appId, string version)
    {
        return $"/{appId}/{version}/graphql";
    }
    
    private string GetGraphQLPlaygroundPath(string appId, string version)
    {
        return $"/{appId}/{version}/ui/playground";
    }

    [ExceptionHandler([typeof(HttpOperationException)], TargetType = typeof(KubernetesAppManager),
        MethodName = nameof(HandleHttpOperationExceptionAsync))]
    [ExceptionHandler(typeof(Exception), TargetType = typeof(KubernetesAppManager),
        MethodName = nameof(HandleExceptionAsync))]
    public virtual async Task<bool> ExistsServiceMonitorAsync(string serviceMonitorName)
    {
        var serviceMonitors = await _kubernetesClientAdapter.ListServiceMonitorAsync(KubernetesConstants.MonitorGroup,
            KubernetesConstants.CoreApiVersion, KubernetesConstants.AppNameSpace, KubernetesConstants.MonitorPlural);
        if (serviceMonitors == null)
        {
            _logger.LogError("Failed to retrieve service monitors, the result is null");
            return false;
        }

        var serviceMonitorList = ((JsonElement)serviceMonitors).Deserialize<ServiceMonitorList>();
        foreach (var serviceMonitor in serviceMonitorList!.Items)
        {
            if (serviceMonitor.Metadata.Name == serviceMonitorName)
                return true;
        }

        return false;
    }

    public async Task DestroyAppAsync(string appId, string version, List<string> chainIds)
    {
        await DestroyAppFullPodsAsync(appId, version, null);
        foreach (var chainId in chainIds)
        {
            await DestroyAppFullPodsAsync(appId, version, chainId);
        }

        await DestroyAppQueryPodsAsync(appId, version);
    }

    private async Task DestroyAppFullPodsAsync(string appId, string version, string chainId)
    {
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);

        //Delete full app deployment
        var fullTypeAppDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeFull, chainId);
        var fullTypeAppDeploymentExists =
            deployments.Items.Any(item => item.Metadata.Name == fullTypeAppDeploymentName);
        if (fullTypeAppDeploymentExists)
        {
            // Delete the existing Deployment
            await _kubernetesClientAdapter.DeleteDeploymentAsync(
                fullTypeAppDeploymentName,
                KubernetesConstants.AppNameSpace
            );
            _logger.LogInformation("[KubernetesAppManager]Deployment {fullTypeAppDeploymentName} deleted.",
                fullTypeAppDeploymentName);
        }

        //Delete full app appsetting config map
        var fullTypeAppConfigMapName =
            ConfigMapHelper.GetAppSettingConfigMapName(appId, version, KubernetesConstants.AppClientTypeFull, chainId);
        var fullTypeAppConfigMapExists =
            configMaps.Items.Any(configMap => configMap.Metadata.Name == fullTypeAppConfigMapName);
        if (fullTypeAppConfigMapExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(fullTypeAppConfigMapName,
                KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {fullTypeAppConfigMapName} deleted.",
                fullTypeAppConfigMapName);
        }

        //Delete full app filebeat config map
        var fullTypeAppSideCarConfigName =
            ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version, KubernetesConstants.AppClientTypeFull, chainId);
        var fullTypeAppSideCarConfigExists =
            configMaps.Items.Any(configMap => configMap.Metadata.Name == fullTypeAppSideCarConfigName);
        if (fullTypeAppSideCarConfigExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(fullTypeAppSideCarConfigName,
                KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {fullTypeAppSideCarConfigName} deleted.",
                fullTypeAppSideCarConfigName);
        }
    }

    private async Task DestroyAppQueryPodsAsync(string appId, string version)
    {
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);

        //Delete query app deployment
        var queryTypeAppDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var queryTypeAppDeploymentExists =
            deployments.Items.Any(item => item.Metadata.Name == queryTypeAppDeploymentName);
        if (queryTypeAppDeploymentExists)
        {
            // Delete the existing Deployment
            await _kubernetesClientAdapter.DeleteDeploymentAsync(
                queryTypeAppDeploymentName,
                KubernetesConstants.AppNameSpace
            );
            _logger.LogInformation("[KubernetesAppManager]Deployment {queryTypeAppDeploymentName} deleted.",
                queryTypeAppDeploymentName);
        }

        //Delete query app appsetting config map
        var queryTypeAppConfigMapName =
            ConfigMapHelper.GetAppSettingConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var queryTypeAppConfigMapExists =
            configMaps.Items.Any(configMap => configMap.Metadata.Name == queryTypeAppConfigMapName);
        if (queryTypeAppConfigMapExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(queryTypeAppConfigMapName,
                KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {queryTypeAppConfigMapName} deleted.",
                queryTypeAppConfigMapName);
        }

        //Delete query app filebeat config map
        var queryTypeAppSideCarConfigName =
            ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var queryTypeAppSideCarConfigExists =
            configMaps.Items.Any(configMap => configMap.Metadata.Name == queryTypeAppSideCarConfigName);
        if (queryTypeAppSideCarConfigExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(queryTypeAppSideCarConfigName,
                KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {queryTypeAppSideCarConfigName} deleted.",
                queryTypeAppSideCarConfigName);
        }

        //Delete query app service
        var serviceName = ServiceHelper.GetAppServiceName(appId, version);
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        var sericeExists = services.Items.Any(item => item.Metadata.Name == serviceName);
        if (sericeExists)
        {
            await _kubernetesClientAdapter.DeleteServiceAsync(serviceName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Service {serviceName} deleted.", serviceName);
        }

        //Delete query app ingress
        var ingressName = IngressHelper.GetAppIngressName(appId, version);
        var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
        var ingressExists = ingresses.Items.Any(item => item.Metadata.Name == ingressName);
        if (ingressExists)
        {
            await _kubernetesClientAdapter.DeleteIngressAsync(ingressName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Ingress {ingressName} deleted.", ingressName);
        }

        //Delete query app service monitor
        // var serviceMonitorName = ServiceMonitorHelper.GetAppServiceMonitorName(appId);
        // var serviceMonitorExists = await ExistsServiceMonitorAsync(serviceMonitorName);
        // if (serviceMonitorExists)
        // {
        //     await _kubernetesClientAdapter.DeleteServiceMonitorAsync(KubernetesConstants.MonitorGroup,
        //         KubernetesConstants.CoreApiVersion, KubernetesConstants.AppNameSpace, KubernetesConstants.MonitorPlural,
        //         serviceMonitorName);
        //     _logger.LogInformation("[KubernetesAppManager]ServiceMonitor {serviceMonitorName} deleted.", serviceMonitorName);
        // }
    }

    public async Task RestartAppAsync(string appId, string version, List<string> chainIds)
    {
        //Restart Full Client Type App Pod
        if (chainIds.Count == 0)
        {
            await RestartAppFullPodsAsync(appId, version, null);
        }
        else
        {
            foreach (var chainId in chainIds)
            {
                await RestartAppFullPodsAsync(appId, version, chainId);
            }
        }

        //Restart Query Client Type App Pod
        await RestartAppQueryPodsAsync(appId, version);
    }

    public async Task RestartAppFullPodsAsync(string appId, string version, string chainId)
    {
        var fullClientDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeFull, chainId);
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var fullClientDeploymentExists = deployments.Items.Any(item => item.Metadata.Name == fullClientDeploymentName);
        if (fullClientDeploymentExists)
        {
            var deployment =
                await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(fullClientDeploymentName,
                    KubernetesConstants.AppNameSpace);
            // Add or update annotations to trigger rolling updates
            var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
            annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
            deployment.Spec.Template.Metadata.Annotations = annotations;

            // Update Deployment
            await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, fullClientDeploymentName,
                KubernetesConstants.AppNameSpace);
        }
        else
        {
            _logger.LogError($"Deployment {fullClientDeploymentName} is not exists!");
        }
    }

    public async Task RestartAppQueryPodsAsync(string appId, string version)
    {
        var queryClientDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var queryClientDeploymentExists =
            deployments.Items.Any(item => item.Metadata.Name == queryClientDeploymentName);
        if (queryClientDeploymentExists)
        {
            var deployment =
                await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(queryClientDeploymentName,
                    KubernetesConstants.AppNameSpace);
            // Add or update annotations to trigger rolling updates
            var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
            annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
            deployment.Spec.Template.Metadata.Annotations = annotations;

            // Update Deployment
            await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, queryClientDeploymentName,
                KubernetesConstants.AppNameSpace);
        }
        else
        {
            _logger.LogError($"Deployment {queryClientDeploymentName} is not exists!");
        }
    }

    public async Task UpdateAppDockerImageAsync(string appId, string version, string newImage, List<string> chainIds,
        bool isUpdateConfig = false)
    {
        //Update full pod docker image
        if (chainIds.Count == 0)
        {
            if (isUpdateConfig)
            {
                await UpdateAppSettingConfigMapAsync(appId, version, KubernetesConstants.AppClientTypeFull, null);
            }

            await UpdateAppDockerImageAsync(appId, version, newImage, KubernetesConstants.AppClientTypeFull, null);
        }
        else
        {
            foreach (var chainId in chainIds)
            {
                if (isUpdateConfig)
                {
                    await UpdateAppSettingConfigMapAsync(appId, version, KubernetesConstants.AppClientTypeFull,
                        chainId);
                }

                await UpdateAppDockerImageAsync(appId, version, newImage, KubernetesConstants.AppClientTypeFull,
                    chainId);
            }
        }

        //Publish app pod update eto to background worker
        await _distributedEventBus.PublishAsync(new AppPodUpdateEto()
        {
            AppId = appId,
            Version = version,
            DockerImage = newImage
        });

        if (isUpdateConfig)
        {
            await UpdateAppSettingConfigMapAsync(appId, version, KubernetesConstants.AppClientTypeQuery, null);
        }

        //Update query pod docker image
        await UpdateAppDockerImageAsync(appId, version, newImage, KubernetesConstants.AppClientTypeQuery, null);
    }

    private async Task UpdateAppDockerImageAsync(string appId, string version, string newImage, string clientType,
        string chainId)
    {
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, clientType, chainId);
        var deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (deploymentExists)
        {
            var deployment =
                await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(deploymentName,
                    KubernetesConstants.AppNameSpace);
            // Add or update annotations to trigger rolling updates
            var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
            annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
            deployment.Spec.Template.Metadata.Annotations = annotations;
            //Update container image 
            var containers = deployment.Spec.Template.Spec.Containers;
            var containerName =
                ContainerHelper.GetAppContainerName(appId, version, clientType, chainId);

            var container = containers.FirstOrDefault(c => c.Name == containerName);
            if (container != null)
            {
                container.Image = newImage;
                await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, deploymentName,
                    KubernetesConstants.AppNameSpace);
                _logger.LogInformation($"Updated deployment {deploymentName} to use image {newImage}");
            }
            else
            {
                _logger.LogError($"Container {containerName} not found in deployment {deploymentName}");
            }
        }
        else
        {
            _logger.LogError($"Deployment {deploymentName} does not exist!");
        }
    }

    private async Task UpdateAppSettingConfigMapAsync(string appId, string version, string clientType, string chainId)
    {
        var appSettingConfigMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version, clientType, chainId);
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        var configMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == appSettingConfigMapName);
        if (!configMapExists)
        {
            _logger.LogError($"ConfigMap {appSettingConfigMapName} does not exist!");
            return;
        }

        var appSettingsContent = File.ReadAllText(KubernetesConstants.AppSettingTemplateFilePath);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderVersion, version);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderClientType,
            clientType);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderChainId, chainId);
        var resourceLimitInfo = await _appResourceLimitProvider.GetAppResourceLimitAsync(appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxEntityCallCount,
            resourceLimitInfo.MaxEntityCallCount.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxEntitySize,
            resourceLimitInfo.MaxEntitySize.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxLogCallCount,
            resourceLimitInfo.MaxLogCallCount.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxLogSize,
            resourceLimitInfo.MaxLogSize.ToString());
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderMaxContractCallCount,
            resourceLimitInfo.MaxContractCallCount.ToString());
        var clientName = $"AeFinder_App-{appId}-{version}";
        if (!chainId.IsNullOrWhiteSpace())
        {
            clientName += $"-{chainId}";
        }

        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderEventBusClientName,
            clientName);
        var exchangeName = $"AeFinder_App-{version}";
        if (!chainId.IsNullOrWhiteSpace())
        {
            exchangeName += $"-{chainId}";
        }

        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderEventBusExchangeName,
            exchangeName);

        var newAppSettingConfigMap =
            ConfigMapHelper.CreateAppSettingConfigMapDefinition(appSettingConfigMapName, appSettingsContent);

        var updatedConfigMap = await _kubernetesClientAdapter.ReplaceNamespacedConfigMapAsync(newAppSettingConfigMap,
            appSettingConfigMapName, KubernetesConstants.AppNameSpace);

        _logger.LogInformation($"Updated app setting config map {appSettingConfigMapName} successfully");
    }


    public async Task<AppPodsPageResultDto> GetPodListWithPagingAsync(string appId, int pageSize, string continueToken)
    {
        V1PodList pods;
        string newContinueToken = null;

        if (string.IsNullOrEmpty(appId))
        {
            (pods, newContinueToken) = await _kubernetesClientAdapter.ListPodsInNamespaceWithPagingAsync(
                KubernetesConstants.AppNameSpace, pageSize, continueToken);
        }
        else
        {
            string labelSelector = $"{KubernetesConstants.AppIdLabelKey}={appId}";
            pods = await _kubernetesClientAdapter.ListPodsInNamespaceWithPagingAsync(KubernetesConstants.AppNameSpace,
                labelSelector);
        }

        var podList = new List<AppPodInfoDto>();
        foreach (var pod in pods.Items)
        {
            var info = new AppPodInfoDto();
            info.PodUid = pod.Metadata.Uid;
            info.PodName = pod.Metadata.Name;
            if (pod.Metadata.Labels.ContainsKey(KubernetesConstants.AppIdLabelKey))
            {
                info.AppId = pod.Metadata.Labels[KubernetesConstants.AppIdLabelKey];
                info.AppVersion = pod.Metadata.Labels[KubernetesConstants.AppVersionLabelKey];
            }

            info.Status = pod.Status.Phase;
            info.PodIP = pod.Status.PodIP;
            info.NodeName = pod.Spec.NodeName;
            info.StartTime = pod.Status.StartTime;
            info.ReadyContainersCount = (pod.Status.ContainerStatuses == null
                ? 0
                : pod.Status.ContainerStatuses.Count(cs => cs.Ready));
            info.TotalContainersCount = (pod.Status.ContainerStatuses == null ? 0 : pod.Status.ContainerStatuses.Count);
            if (pod.Metadata.CreationTimestamp.HasValue)
            {
                var creationTime = pod.Metadata.CreationTimestamp.Value;
                TimeSpan age = DateTime.Now - creationTime;
                info.AgeSeconds = age.TotalSeconds;
            }

            var containerList = new List<PodContainerDto>();
            foreach (var v1Container in pod.Spec.Containers)
            {
                var container = new PodContainerDto();
                container.ContainerName = v1Container.Name;
                var requests = v1Container.Resources.Requests;
                if (requests != null)
                {
                    if (requests.ContainsKey("cpu"))
                    {
                        container.RequestCpu = requests["cpu"].ToString();
                    }

                    if (requests.ContainsKey("memory"))
                    {
                        container.RequestMemory = requests["memory"].ToString();
                    }
                }
                container.ContainerImage = v1Container.Image;
                containerList.Add(container);
            }

            if (pod.Status.ContainerStatuses != null)
            {
                foreach (var v1ContainerStatus in pod.Status.ContainerStatuses)
                {
                    var container = containerList.Find(c => c.ContainerName == v1ContainerStatus.Name);
                    container.ContainerID = v1ContainerStatus.ContainerID;
                    container.RestartCount = v1ContainerStatus.RestartCount;
                    container.Ready = v1ContainerStatus.Ready;
                    container.CurrentState = (v1ContainerStatus.State?.Running != null ? "Running" : "Not Running");
                }
            }

            info.Containers = containerList;
            podList.Add(info);
        }

        return new AppPodsPageResultDto()
        {
            ContinueToken = newContinueToken,
            PodInfos = podList
        };
    }

    public async Task UpdateAppFullPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory, List<string> chainIds, string limitCpu, string limitMemory)
    {
        if (chainIds.Count == 0)
        {
            await UpdateAppResourceAsync(appId, version, requestCpu, requestMemory,
                KubernetesConstants.AppClientTypeFull, null, limitCpu, limitMemory, 0);
        }
        else
        {
            foreach (var chainId in chainIds)
            {
                await UpdateAppResourceAsync(appId, version, requestCpu, requestMemory,
                    KubernetesConstants.AppClientTypeFull, chainId, limitCpu, limitMemory, 0);
            }
        }
    }

    public async Task UpdateAppQueryPodResourceAsync(string appId, string version, string requestCpu,
        string requestMemory, string limitCpu, string limitMemory, int replicasCount)
    {
        await UpdateAppResourceAsync(appId, version, requestCpu, requestMemory,
            KubernetesConstants.AppClientTypeQuery, null, limitCpu, limitMemory, replicasCount);
    }

    private async Task UpdateAppResourceAsync(string appId, string version, string requestCpu, string requestMemory,
        string clientType, string chainId, string limitCpu, string limitMemory, int replicasCount)
    {
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        var deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, clientType, chainId);
        var deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (deploymentExists)
        {
            var deployment =
                await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(deploymentName,
                    KubernetesConstants.AppNameSpace);
            // Add or update annotations to trigger rolling updates
            var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
            annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
            deployment.Spec.Template.Metadata.Annotations = annotations;
            //Update pods count
            if (replicasCount > 0)
            {
                deployment.Spec.Replicas = replicasCount;
            }
            
            //Update container resource
            var containers = deployment.Spec.Template.Spec.Containers;
            var containerName =
                ContainerHelper.GetAppContainerName(appId, version, clientType, chainId);

            var container = containers.FirstOrDefault(c => c.Name == containerName);
            if (container != null)
            {
                var resources = DeploymentHelper.CreateResources(requestCpu, requestMemory, limitCpu, limitMemory);
                container.Resources = resources;
                await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, deploymentName,
                    KubernetesConstants.AppNameSpace);
                _logger.LogInformation(
                    $"Updated deployment {deploymentName} main container resources cpu: {requestCpu} memory: {requestMemory}");
            }
            else
            {
                _logger.LogError($"Container {containerName} not found in deployment {deploymentName}");
            }
        }
        else
        {
            _logger.LogError($"Deployment {deploymentName} does not exist!");
        }
    }

    public async Task<AppPodOperationSnapshotDto> GetPodResourceSnapshotAsync(string appId, string version)
    {
        string labelSelector =
            $"{KubernetesConstants.AppIdLabelKey}={appId},{KubernetesConstants.AppVersionLabelKey}={version}";
        var pods = await _kubernetesClientAdapter.ListPodsInNamespaceWithPagingAsync(KubernetesConstants.AppNameSpace,
            labelSelector);

        var result = new AppPodOperationSnapshotDto();
        result.AppId = appId;
        result.AppVersion = version;
        result.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        result.PodNameList = new List<string>();
        bool isMultipleInstance = false;
        var chainIdList = new List<string>();
        foreach (var pod in pods.Items)
        {
            var podName = pod.Metadata.Name;
            result.PodNameList.Add(podName);
            bool isFullPod = CheckIsFullPod(pod);
            if (isFullPod)
            {
                var chainId = GetPodChainId(pod);
                if (!string.IsNullOrEmpty(chainId))
                {
                    isMultipleInstance = true;
                    if (!chainIdList.Contains(chainId))
                    {
                        chainIdList.Add(chainId);
                        result.AppFullPodCount += 1;
                    }
                }
            }
            else
            {
                result.AppQueryPodReplicas += 1;
            }

            foreach (var container in pod.Spec.Containers)
            {
                if (container.Name == KubernetesConstants.FileBeatContainerName)
                {
                    continue;
                }
                
                var requests = container.Resources.Requests;
                var limits = container.Resources.Limits;
                
                if (isFullPod)
                {
                    result.AppFullPodRequestCpuCore = requests.ContainsKey("cpu") ? requests["cpu"].ToString() : "";
                    result.AppFullPodRequestMemory =
                        requests.ContainsKey("memory") ? requests["memory"].ToString() : "";
                    result.AppFullPodLimitCpuCore = (limits!=null && limits.ContainsKey("cpu")) ? limits["cpu"].ToString() : "";
                    result.AppFullPodLimitMemory = (limits!=null && limits.ContainsKey("memory")) ? limits["memory"].ToString() : "";
                }
                else
                {
                    result.AppQueryPodRequestCpuCore = requests.ContainsKey("cpu") ? requests["cpu"].ToString() : "";
                    result.AppQueryPodRequestMemory =
                        requests.ContainsKey("memory") ? requests["memory"].ToString() : "";
                    result.AppQueryPodLimitCpuCore = (limits!=null && limits.ContainsKey("cpu")) ? limits["cpu"].ToString() : "";
                    result.AppQueryPodLimitMemory = (limits!=null && limits.ContainsKey("memory")) ? limits["memory"].ToString() : "";
                }
            }
            
        }

        if (!isMultipleInstance)
        {
            result.AppFullPodCount = 1;
        }

        return result;
    }

    private bool CheckIsFullPod(V1Pod pod)
    {
        if (pod.Metadata.Labels.ContainsKey(KubernetesConstants.AppPodTypeLabelKey))
        {
            string podType = pod.Metadata.Labels[KubernetesConstants.AppPodTypeLabelKey];
            if (podType == KubernetesConstants.AppClientTypeFull)
            {
                return true;
            }

            return false;
        }

        // throw new Exception("Unable to recognize pod type");
        return false;
    }

    private string GetPodChainId(V1Pod pod)
    {
        if (pod.Metadata.Labels.ContainsKey(KubernetesConstants.AppPodChainIdLabelKey))
        {
            var podChainId = pod.Metadata.Labels[KubernetesConstants.AppPodChainIdLabelKey];
            return podChainId;
        }

        return null;
    }
}