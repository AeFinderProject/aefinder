namespace AElfIndexer.Grains.State.BlockScanExecution;

public class ClientManagerState
{
    public Dictionary<string, HashSet<string>> ClientIds { get; set; } = new();
}