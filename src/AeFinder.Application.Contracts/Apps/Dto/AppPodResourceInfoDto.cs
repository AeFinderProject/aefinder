using System;
using System.Collections.Generic;

namespace AeFinder.Apps.Dto;

public class AppPodResourceInfoDto
{
    public string PodUid { get; set; }
    public string PodName { get; set; }
    public DateTime? CurrentTime { get; set; }
    public List<PodContainerResourceDto> Containers { get; set; }
}

public class PodContainerResourceDto
{
    public string ContainerName { get; set; }
    public string CpuUsage { get; set; }
    public string MemoryUsage { get; set; }
}