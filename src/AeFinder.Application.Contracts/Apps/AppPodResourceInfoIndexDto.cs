using System;
using System.Collections.Generic;

namespace AeFinder.Apps;

public class AppPodResourceInfoIndexDto
{
    public string PodUid { get; set; }
    public string PodName { get; set; }
    public string AppId { get; set; }
    public string AppVersion { get; set; }
    public string Status { get; set; }
    public DateTime? StartTime { get; set; }
    public int ReadyContainersCount { get; set; }
    public int TotalContainersCount { get; set; }
    public double AgeSeconds { get; set; }
    public string CpuUsage { get; set; }
    public string MemoryUsage { get; set; }
    public List<PodContainerInfoDto> Containers { get; set; }
}

public class PodContainerInfoDto
{
    public string ContainerID { get; set; }
    public string ContainerName { get; set; }
    public int RestartCount { get; set; }
    public bool Ready { get; set; }
    public string CurrentState { get; set; }
    public string RequestCpu { get; set; }
    public string RequestMemory { get; set; }
    public string CpuUsage { get; set; }
    public string MemoryUsage { get; set; }
}