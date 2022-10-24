namespace AElfScan.Grain.ScanClients;

public class ClientManagerState
{
    public Dictionary<string, HashSet<string>> ClientIds { get; set; } = new();
}