using Microsoft.Extensions.Configuration;

namespace AeFinder.Kubernetes;

public class KubernetesConstants
{
     public const string CoreApiVersion = "v1";
     public const string NginxIngressClassName = "nginx";
     //resource definition
     // public const string AppNameSpace = "aefinder-app";
     public const string NodeAffinityValue = "aefinder-app";
     public static string AppNameSpace { get; private set; }
     public const string AppLabelKey = "app";
     public const string AppIdLabelKey = "app-id";
     public const string AppVersionLabelKey = "app-version";
     public const string AppPodTypeLabelKey = "app-pod-type";
     public const string AppPodChainIdLabelKey = "app-pod-chainid";
     public const string AppSettingFileName = "appsettings.json";
     public const string AppSettingFileMountPath = "/app/appsettings.json";
     public const string AppLogFileMountPath = "/app/Logs";
     
     //FileBeat
     public const string FileBeatImage = "docker.elastic.co/beats/filebeat:7.16.2";
     public const string FileBeatConfigMountPath = "/etc/filebeat/filebeat.yml";
     public const string FileBeatConfigFileName = "filebeat.yml";
     public const string FileBeatLogILMPolicyName = "filebeat-log-policy";
     public const string FileBeatContainerName = "filebeat-sidecar";
     
     //manager
     public const string AppClientTypeFull = "Full";
     public const string AppClientTypeQuery = "Query";
     public const string AppSettingTemplateFilePath = "AppConfigTemplate/appsettings-template.json";
     public const string AppFileBeatConfigTemplateFilePath = "AppConfigTemplate/filebeat-template.yml";
     public const string PlaceHolderAppId = "[AppId]";
     public const string PlaceHolderVersion = "[Version]";
     public const string PlaceHolderClientType = "[ClientType]";
     public const string PlaceHolderChainId = "[ChainId]";
     public const string PlaceHolderNameSpace = "[NameSpace]";
     public const int AppContainerTargetPort = 8308;
     public const string PlaceHolderMaxEntityCallCount = "[MaxEntityCallCount]";
     public const string PlaceHolderMaxEntitySize = "[MaxEntitySize]";
     public const string PlaceHolderMaxLogCallCount = "[MaxLogCallCount]";
     public const string PlaceHolderMaxLogSize = "[MaxLogSize]";
     public const string PlaceHolderMaxContractCallCount = "[MaxContractCallCount]";
     public const string FullPodMaxSurge = "0";
     public const string FullPodMaxUnavailable = "1";
     public const string QueryPodMaxSurge = "50%";
     public const string QueryPodMaxUnavailable = "0";
     public const string PlaceHolderEventBusClientName = "[EventBusClientName]";
     public const string PlaceHolderEventBusExchangeName = "[EventBusExchangeName]";
     
     //Prometheus
     public const string MonitorLabelKey = "monitor";
     public const string MonitorGroup = "monitoring.coreos.com";
     public const string MonitorPlural = "servicemonitors";
     public const string MetricsPath = "/metrics";
     
     public static void Initialize(IConfiguration configuration)
     {
          AppNameSpace = configuration["Kubernetes:AppNameSpace"] ?? "aefinder-app";
     }
}