namespace AElfScan.Orleans.EventSourcing.Grain.ScanClients;

public class ScanModeInfo
{
    public ScanMode ScanMode { get; set; }
    public long ScanNewBlockStartHeight {get;set;}
}