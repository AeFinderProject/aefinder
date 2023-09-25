namespace AElfIndexer.Grains.Grain.BlockScan;

public class BlockScanOptions
{
    public int BatchPushBlockCount { get; set; } = 100;
    public int ScanHistoryBlockThreshold { get; set; } = 1000;
    public int HistoricalPushRecoveryThreshold { get; set; } = 5;
    public int BatchPushNewBlockCount { get; set; } = 10;
    public int MaxHistoricalBlockPushThreshold { get; set; } = 10000;
    public int MaxNewBlockPushThreshold { get; set; }  = 5000;
}