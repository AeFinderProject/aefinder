namespace AeFinder.App.OperationLimits;

public class OperationLimitOptions
{
    public int MaxEntityCallCount { get; set; } = 100;
    public int MaxEntitySize { get; set; } = 100000;
    public int MaxLogCallCount { get; set; } = 100;
    public int MaxLogSize { get; set; } = 100000;
    public int MaxContractCallCount { get; set; } = 100;
}