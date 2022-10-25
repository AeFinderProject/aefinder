namespace AElfScan.Orleans.EventSourcing.Grain.ScanClients;

public class Block
{
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
    public string PreviousBlockHash { get; set; }
}