namespace AElfIndexer.Grains.State.Client;

public class BlockStateSetManagerState<T>
{
    public string LongestChainBlockHash { get; set; }
    public string BestChainBlockHash { get; set; }
    public Dictionary<string, HashSet<string>> BlockStateSets { get; set; } = new();
}