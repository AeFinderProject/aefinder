namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public class ScanModeInfo
{
    public ScanMode ScanMode { get; set; }
    public long ScanNewBlockStartHeight {get;set;}
}