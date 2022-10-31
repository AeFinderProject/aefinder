namespace AElfScan.Orleans.EventSourcing.State.BlockScan;

public class ScanModeInfo
{
    public ScanMode ScanMode { get; set; }
    public long ScanNewBlockStartHeight {get;set;}
}