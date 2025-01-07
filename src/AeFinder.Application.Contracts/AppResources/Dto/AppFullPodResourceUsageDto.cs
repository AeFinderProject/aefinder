namespace AeFinder.AppResources.Dto;

public class AppFullPodResourceUsageDto
{
    public string AppId { get; set; }
    public string AppVersion { get; set; }
    public string ContainerName { get; set; }
    public string CurrentState { get; set; }
    public string RequestCpu { get; set; }
    public string RequestMemory { get; set; }
    public string LimitCpu { get; set; }
    public string LimitMemory { get; set; }
    public long UsageTimestamp { get; set; }
    public string CpuUsage { get; set; }
    public string MemoryUsage { get; set; }
}