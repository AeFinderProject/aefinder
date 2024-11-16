using AeFinder.Apps;

namespace AeFinder.Grains.State.Apps;

[GenerateSerializer]
public class AppPodOperationSnapshotState
{
    [Id(0)] public string AppId { get; set; }
    [Id(1)] public string AppVersion { get; set; }
    [Id(2)] public long Timestamp { get; set; }
    [Id(3)] public string AppFullPodRequestCpuCore { get; set; }
    [Id(4)] public string AppFullPodRequestMemory { get; set; }
    [Id(5)] public string AppQueryPodRequestCpuCore { get; set; }
    [Id(6)] public string AppQueryPodRequestMemory { get; set; }
    [Id(7)] public List<string> PodNameList { get; set; }
    [Id(8)] public int AppFullPodCount { get; set; }
    [Id(9)] public int AppQueryPodReplicas { get; set; }
    [Id(10)] public string AppFullPodLimitCpuCore { get; set; }
    [Id(11)] public string AppFullPodLimitMemory { get; set; }
    [Id(12)] public string AppQueryPodLimitCpuCore { get; set; }
    [Id(13)] public string AppQueryPodLimitMemory { get; set; }
    [Id(14)] public AppPodOperationType PodOperationType { get; set; }
}