namespace AElfIndexer.Grains.Grain.BlockScan;

public class BlockScanOptions
{
    public int BatchPushBlockCount { get; set; } = 100;
    public int ScanHistoryBlockThreshold { get; set; } = 1000;
    public int HistoricalPushRecoveryThreshold { get; set; } = 5;

    public int PushNewBlockCount { get; set; } = 10;
}