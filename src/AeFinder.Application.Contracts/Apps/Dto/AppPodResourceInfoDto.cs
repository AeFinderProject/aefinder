using System;
using System.Collections.Generic;

namespace AeFinder.Apps.Dto;

public class AppPodResourceInfoDto
{
    public string PodName { get; set; }
    public long Timestamp { get; set; }
    public string CpuUsage { get; set; }
    public string MemoryUsage { get; set; }
    public List<PodContainerResourceDto> Containers { get; set; }
}

public class PodContainerResourceDto
{
    public string ContainerName { get; set; }
    public long Timestamp { get; set; }
    public string CpuUsage { get; set; }
    public string MemoryUsage { get; set; }
}