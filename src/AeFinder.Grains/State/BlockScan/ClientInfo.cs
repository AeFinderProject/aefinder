namespace AeFinder.Grains.State.BlockScan;

public class ClientInfo
{
    public string Version { get; set; }
    public string ChainId { get; set; }
    public string ClientId { get; set; }
    public DateTime LastHandleHistoricalBlockTime { get; set; }
    public ScanModeInfo ScanModeInfo { get; set; }= new();
}