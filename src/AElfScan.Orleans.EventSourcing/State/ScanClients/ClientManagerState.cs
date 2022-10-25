namespace AElfScan.Orleans.EventSourcing.State.ScanClients;

public class ClientManagerState
{
    public Dictionary<string, HashSet<string>> ClientIds { get; set; } = new();
}