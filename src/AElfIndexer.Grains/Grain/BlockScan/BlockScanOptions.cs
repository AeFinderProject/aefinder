namespace AElfIndexer.Grains.Grain.BlockScan;

public class BlockScanOptions
{
    public int BatchPushBlockCount { get; set; } = 100;
    public int ScanHistoryBlockThreshold { get; set; } = 1000;
    public int HistoricalPushRecoveryThreshold { get; set; } = 5;
    public int BatchPushNewBlockCount { get; set; } = 10;
    public int MaxHistoricalBlockPushThreshold = 10000;
    public int MaxNewBlockPushThreshold = 5000;
}