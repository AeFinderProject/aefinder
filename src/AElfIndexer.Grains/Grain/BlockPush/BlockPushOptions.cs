namespace AElfIndexer.Grains.Grain.BlockPush;

public class BlockPushOptions
{
    public int BatchPushBlockCount { get; set; } = 100;
    public int PushHistoryBlockThreshold { get; set; } = 1000;
    public int HistoricalPushRecoveryThreshold { get; set; } = 5;
    public int BatchPushNewBlockCount { get; set; } = 10;
    public int MaxHistoricalBlockPushThreshold { get; set; } = 10000;
    public int MaxNewBlockPushThreshold { get; set; }  = 5000;
}