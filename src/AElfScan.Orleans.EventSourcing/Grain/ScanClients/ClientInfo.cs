namespace AElfScan.Orleans.EventSourcing.Grain.ScanClients;

public class ClientInfo
{
    public string Version { get; set; }
    public string ChainId { get; set; }
    public string ClientId { get; set; }
    public ScanModeInfo ScanModeInfo { get; set; }= new();
}