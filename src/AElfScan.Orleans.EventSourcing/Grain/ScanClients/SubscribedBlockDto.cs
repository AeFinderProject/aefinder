namespace AElfScan.Orleans.EventSourcing.Grain.ScanClients;

public class SubscribedBlockDto
{
    public string ClientId { get; set; }
    public List<Block> Blocks { get; set; }
}