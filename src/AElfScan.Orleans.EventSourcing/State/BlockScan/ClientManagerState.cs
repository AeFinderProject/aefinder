namespace AElfScan.Orleans.EventSourcing.State.BlockScan;

public class ClientManagerState
{
    public Dictionary<string, HashSet<string>> ClientIds { get; set; } = new();
}