namespace AElfIndexer.Grains.State.Client;

public class BlockStateSetState
{
    public string LongestChainBlockHash { get; set; }
    public string BestChainBlockHash { get; set; }
    public string BlockStateSetVersion { get; set; }
    public Dictionary<string, HashSet<string>> BlockStateSets { get; set; } = new();
}