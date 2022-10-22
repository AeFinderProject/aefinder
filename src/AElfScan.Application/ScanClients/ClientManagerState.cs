using System.Collections.Generic;

namespace AElfScan.ScanClients;

public class ClientManagerState
{
    public Dictionary<string, HashSet<string>> ClientIds { get; set; } = new();
}