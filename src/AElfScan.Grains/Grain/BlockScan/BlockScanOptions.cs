namespace AElfScan.Grains.Grain.BlockScan;

public class BlockScanOptions
{
    public int BatchPushBlockCount { get; set; } = 100;
    public int ScanHistoryBlockThreshold { get; set; } = 200;
}