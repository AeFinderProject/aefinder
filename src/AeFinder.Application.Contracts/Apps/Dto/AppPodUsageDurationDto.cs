using System;
using System.Collections.Generic;

namespace AeFinder.Apps.Dto;

public class AppPodUsageDurationDto
{
    public string AppId { get; set; }
    public string AppVersion { get; set; }
    public long StartTimestamp { get; set; }
    public long EndTimestamp { get; set; }
    public DateTime StartTime {
        get
        {
            return DateTime.UnixEpoch.AddMilliseconds(StartTimestamp);
        }
    }
    public DateTime EndTime {
        get
        {
            return DateTime.UnixEpoch.AddMilliseconds(EndTimestamp);
        }
    }
    public long TotalUsageDuration { get; set; }
    public string AppFullPodRequestCpuCore { get; set; }
    public string AppFullPodRequestMemory { get; set; }
    public string AppQueryPodRequestCpuCore { get; set; }
    public string AppQueryPodRequestMemory { get; set; }
    public List<string> PodNameList { get; set; }
    public int AppFullPodCount { get; set; }
    public int AppQueryPodReplicas { get; set; }
    public string AppFullPodLimitCpuCore { get; set; }
    public string AppFullPodLimitMemory { get; set; }
    public string AppQueryPodLimitCpuCore { get; set; }
    public string AppQueryPodLimitMemory { get; set; }
}