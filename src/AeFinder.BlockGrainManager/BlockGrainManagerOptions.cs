namespace AeFinder.BlockGrainManager;

public class BlockGrainManagerOptions
{
    public string ChainId { get; set; }
    public long StartHeight { get; set; }
    public long EndHeight { get; set; }
    public int PerSyncBlockCount { get; set; }
}