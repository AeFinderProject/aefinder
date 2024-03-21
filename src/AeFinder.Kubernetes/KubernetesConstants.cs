namespace AeFinder.Kubernetes;

public class KubernetesConstants
{
     public const string NginxIngressClassName = "nginx";
     
     public const string AppNameSpace = "aefinder-app";
     public const string AppLabelKey = "app";
     public const string AppSettingFileName = "appsettings.json";
     public const string AppSettingFileMountPath = "/app/appsettings.json";
     public const string AppLogFileMountPath = "/app/Logs";

     public const string FileBeatImage = "docker.elastic.co/beats/filebeat:7.16.2";
     public const string FileBeatConfigMountPath = "/etc/filebeat/filebeat.yml";
     public const string FileBeatConfigFileName = "filebeat.yml";
}