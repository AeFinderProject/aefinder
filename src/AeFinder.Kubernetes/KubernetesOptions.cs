namespace AeFinder.Kubernetes;

public class KubernetesOptions
{
    public string KubeConfigPath { get; set; } = "KubeConfig/config.txt";
    public int AppPodReplicas { get; set; } = 1;
    public string HostName { get; set; }

}