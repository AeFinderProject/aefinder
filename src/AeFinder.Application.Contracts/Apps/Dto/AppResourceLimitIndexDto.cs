namespace AeFinder.Apps;

public class AppResourceLimitIndexDto
{
    public string AppId { get; set; }
    public string AppName { get; set; }
    public string OrganizationId { get; set; }
    public string OrganizationName { get; set; }
    public ResourceLimitDto ResourceLimit { get; set; }
    public OperationLimitDto OperationLimit { get; set; }
    public DeployLimitInfoDto DeployLimit { get; set; }
}

public class ResourceLimitDto
{
    public string AppFullPodRequestCpuCore { get; set; }
    public string AppFullPodRequestMemory { get; set; }
    public string AppQueryPodRequestCpuCore { get; set; }
    public string AppQueryPodRequestMemory { get; set; }
    public int AppPodReplicas { get; set; }
    public bool EnableMultipleInstances { get; set; }
    public string AppFullPodLimitCpuCore { get; set; }
    public string AppFullPodLimitMemory { get; set; }
}

public class OperationLimitDto
{
    public int MaxEntityCallCount { get; set; }
    public int MaxEntitySize { get; set; }
    public int MaxLogCallCount { get; set; }
    public int MaxLogSize { get; set; }
    public int MaxContractCallCount { get; set; }
}

public class DeployLimitInfoDto
{
    public long MaxAppCodeSize { get; set; }
    public long MaxAppAttachmentSize { get; set; }
}