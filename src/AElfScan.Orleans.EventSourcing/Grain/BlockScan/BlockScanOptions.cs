namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public class BlockScanOptions
{
    public int BatchPushBlockCount { get; set; } = 500;
    public int ScanHistoryBlockThreshold { get; set; } = 200;
}