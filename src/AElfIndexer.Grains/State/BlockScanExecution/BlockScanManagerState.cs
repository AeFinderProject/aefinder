namespace AElfIndexer.Grains.State.BlockScanExecution;

public class BlockScanManagerState
{
    public Dictionary<string, HashSet<string>> BlockScanIds { get; set; } = new();
}