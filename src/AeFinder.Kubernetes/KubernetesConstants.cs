using Microsoft.Extensions.Configuration;

namespace AeFinder.Kubernetes;

public class KubernetesConstants
{
     public const string CoreApiVersion = "v1";
     public const string NginxIngressClassName = "nginx";
     //resource definition
     // public const string AppNameSpace = "aefinder-app";
     public const string NodeAffinityValue = "aefinder";
     public static string AppNameSpace { get; private set; }
     public const string AppLabelKey = "app";
     public const string AppSettingFileName = "appsettings.json";
     public const string AppSettingFileMountPath = "/app/appsettings.json";
     public const string AppLogFileMountPath = "/app/Logs";
     
     public const string FileBeatImage = "docker.elastic.co/beats/filebeat:7.16.2";
     public const string FileBeatConfigMountPath = "/etc/filebeat/filebeat.yml";
     public const string FileBeatConfigFileName = "filebeat.yml";
     
     //manager
     public const string AppClientTypeFull = "Full";
     public const string AppClientTypeQuery = "Query";
     public const string AppSettingTemplateFilePath = "AppConfigTemplate/appsettings-template.json";
     public const string AppFileBeatConfigTemplateFilePath = "AppConfigTemplate/filebeat-template.yml";
     public const string PlaceHolderAppId = "[AppId]";
     public const string PlaceHolderVersion = "[Version]";
     public const string PlaceHolderClientType = "[ClientType]";
     public const string PlaceHolderNameSpace = "[NameSpace]";
     public const int AppContainerTargetPort = 8308;
     
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