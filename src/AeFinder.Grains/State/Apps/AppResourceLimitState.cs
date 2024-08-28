namespace AeFinder.Grains.State.Apps;

[GenerateSerializer]
public class AppResourceLimitState
{
    [Id(0)] public int MaxEntityCallCount { get; set; }
    [Id(1)] public int MaxEntitySize { get; set; }
    [Id(2)] public int MaxLogCallCount { get; set; }
    [Id(3)] public int MaxLogSize { get; set; }
    [Id(4)] public int MaxContractCallCount { get; set; }

    [Id(5)] public string AppFullPodRequestCpuCore { get; set; }
    [Id(6)] public string AppFullPodRequestMemory { get; set; }
    [Id(7)] public string AppQueryPodRequestCpuCore { get; set; }
    [Id(8)] public string AppQueryPodRequestMemory { get; set; }
    [Id(9)] public int AppPodReplicas { get; set; }

    [Id(10)] public bool EnableMultipleInstances { get; set; } = false;
}