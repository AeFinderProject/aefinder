using System;
using System.Collections.Generic;
using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class AppPodInfoIndex : AeFinderEntity<string>, IEntityMappingEntity
{
    [Keyword]
    public override string Id
    {
        get { return PodName; }
    }
    [Keyword]public string PodUid { get; set; }
    [Keyword]public string PodName { get; set; }
    [Keyword]public string AppId { get; set; }
    [Keyword]public string AppVersion { get; set; }
    [Keyword]public string Status { get; set; }
    [Keyword]public string PodIP { get; set; }
    [Keyword]public string NodeName { get; set; }
    public DateTime? StartTime { get; set; }
    public int ReadyContainersCount { get; set; }
    public int TotalContainersCount { get; set; }
    public double AgeSeconds { get; set; }
    public long UsageTimestamp { get; set; }
    [Keyword]public string CpuUsage { get; set; }
    [Keyword]public string MemoryUsage { get; set; }
    public List<PodContainerInfo> Containers { get; set; }
}

public class PodContainerInfo
{
    [Keyword]public string ContainerID { get; set; }
    [Keyword]public string ContainerName { get; set; }
    [Keyword]public string ContainerImage { get; set; }
    public int RestartCount { get; set; }
    public bool Ready { get; set; }
    [Keyword]public string CurrentState { get; set; }
    [Keyword]public string RequestCpu { get; set; }
    [Keyword]public string RequestMemory { get; set; }
    public long UsageTimestamp { get; set; }
    [Keyword]public string CpuUsage { get; set; }
    [Keyword]public string MemoryUsage { get; set; }
}