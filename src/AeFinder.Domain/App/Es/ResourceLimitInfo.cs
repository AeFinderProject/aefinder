using Nest;

namespace AeFinder.App.Es;

public class ResourceLimitInfo
{
    [Keyword] public string AppFullPodRequestCpuCore { get; set; }
    [Keyword] public string AppFullPodRequestMemory { get; set; }
    [Keyword] public string AppQueryPodRequestCpuCore { get; set; }
    [Keyword] public string AppQueryPodRequestMemory { get; set; }
    public int AppPodReplicas { get; set; }
    public bool EnableMultipleInstances { get; set; }
}