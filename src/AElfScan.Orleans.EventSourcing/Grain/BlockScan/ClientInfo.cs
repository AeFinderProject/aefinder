namespace AElfScan.Orleans.EventSourcing.Grain.BlockScan;

public class ClientInfo
{
    public string Version { get; set; }
    public string ChainId { get; set; }
    public string ClientId { get; set; }
    public ScanModeInfo ScanModeInfo { get; set; }= new();
}