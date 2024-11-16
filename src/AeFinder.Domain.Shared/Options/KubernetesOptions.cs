namespace AeFinder.Options;

public class KubernetesOptions
{
    public string KubeConfigPath { get; set; } = "KubeConfig/config.txt";
    public string AppNameSpace { get; set; }
    public int AppPodReplicas { get; set; } = 1;
    public string HostName { get; set; }
    public string OriginName { get; set; }
    public string AppFullPodRequestCpuCore { get; set; } = "1";
    public string AppFullPodRequestMemory { get; set; } = "2Gi";
    public string AppQueryPodRequestCpuCore { get; set; } = "1";
    public string AppQueryPodRequestMemory { get; set; } = "2Gi";
    public string PrometheusUrl { get; set; }
}