namespace AeFinder.Kubernetes;

public class KubernetesOptions
{
    public string KubeConfigPath { get; set; } = "KubeConfig/config.txt";
    public string AppNameSpace { get; set; }
    public int AppPodReplicas { get; set; } = 1;
    public string HostName { get; set; }
    public string OriginName { get; set; }

}