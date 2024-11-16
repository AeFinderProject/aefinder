using System.Collections.Generic;
using AeFinder.Apps;
using AeFinder.Entities;
using AElf.EntityMapping.Entities;
using Nest;

namespace AeFinder.App.Es;

public class AppPodOperationSnapshotIndex: AeFinderEntity<string>, IEntityMappingEntity
{
    [Keyword]public override string Id
    {
        get { return AppId + "-" + AppVersion + "-" + Timestamp; }
    }

    [Keyword]public string AppId { get; set; }
    [Keyword]public string AppVersion { get; set; }
    public long Timestamp { get; set; }
    [Keyword]public string AppFullPodRequestCpuCore { get; set; }
    [Keyword]public string AppFullPodRequestMemory { get; set; }
    [Keyword]public string AppQueryPodRequestCpuCore { get; set; }
    [Keyword]public string AppQueryPodRequestMemory { get; set; }
    public List<string> PodNameList { get; set; }
    public int AppFullPodCount { get; set; }
    public int AppQueryPodReplicas { get; set; }
    [Keyword]public string AppFullPodLimitCpuCore { get; set; }
    [Keyword]public string AppFullPodLimitMemory { get; set; }
    [Keyword]public string AppQueryPodLimitCpuCore { get; set; }
    [Keyword]public string AppQueryPodLimitMemory { get; set; }
    public AppPodOperationType PodOperationType { get; set; }
}