using System.Collections.Generic;

namespace AElfIndexer.Grains.State.BlockScan;

public class ClientManagerState
{
    public Dictionary<string, HashSet<string>> ClientIds { get; set; } = new();
}