namespace AeFinder.Grains.State.BlockScan;

public class ClientManagerState
{
    public Dictionary<string, HashSet<string>> ClientIds { get; set; } = new();
}