using Volo.Abp.EventBus;

namespace AeFinder.Apps.Eto;

[EventName("AeFinder.AppLimitUpdateDto")]
public class AppLimitUpdateEto
{
    public string AppId { get; set; }
    public int MaxEntityCallCount { get; set; }
    public int MaxEntitySize { get; set; }
    public int MaxLogCallCount { get; set; }
    public int MaxLogSize { get; set; }
    public int MaxContractCallCount { get; set; }

    public string AppFullPodRequestCpuCore { get; set; }
    public string AppFullPodRequestMemory { get; set; }
    public string AppQueryPodRequestCpuCore { get; set; }
    public string AppQueryPodRequestMemory { get; set; }
    public int AppPodReplicas { get; set; }
    
    public long MaxAppCodeSize { get; set; }
    public long MaxAppAttachmentSize { get; set; }
    
    public bool EnableMultipleInstances { get; set; }
}