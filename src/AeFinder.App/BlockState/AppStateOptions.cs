namespace AeFinder.App.BlockState;

public class AppStateOptions
{
    public int AppDataCacheCount { get; set; } = 1000;
    public int MaxAppStateBatchCommitCount { get; set; } = 100;
    public int MaxAppIndexBatchCommitCount { get; set; } = 100;
}