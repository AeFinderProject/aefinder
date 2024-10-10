using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;

namespace AeFinder.Apps.Dto;

public class AppPodInfoDto
{
    public string PodUid { get; set; }
    public string PodName { get; set; }
    public string AppId { get; set; }
    public string AppVersion { get; set; }
    public string Status { get; set; }
    public string PodIP { get; set; }
    public string NodeName { get; set; }
    public DateTime? StartTime { get; set; }
    public int ReadyContainersCount { get; set; }
    public int TotalContainersCount { get; set; }
    public long AgeSeconds { get; set; }
    public List<PodContainer> Containers { get; set; }
}

public class PodContainer
{
    public string ContainerID { get; set; }
    public string ContainerName { get; set; }
    public string ContainerImage { get; set; }
    public int RestartCount { get; set; }
    public bool Ready { get; set; }
    public string CurrentState { get; set; }
}