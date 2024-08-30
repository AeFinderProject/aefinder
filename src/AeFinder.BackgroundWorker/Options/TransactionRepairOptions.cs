namespace AeFinder.BackgroundWorker.Options;

public class TransactionRepairOptions
{
    public bool Enable { get; set; } = false;
    public int MaxBlockCount { get; set; } = 100;
    public int Period { get; set; } = 1000;
    public Dictionary<string, TransactionRepairInfo> Chains { get; set; } = new();
}

public class TransactionRepairInfo
{
    public long StartBlockHeight { get; set; }
    public long EndBlockHeight { get; set; }
}
