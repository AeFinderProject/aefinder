namespace AeFinder.Grains.Grain.BlockStates;

public class BlockStateSetStatus
{
    public string LongestChainBlockHash { get; set; }
    public long LongestChainHeight { get; set; }
    public string BestChainBlockHash { get; set; }
    public long BestChainHeight { get; set; }
    public string LastIrreversibleBlockHash { get; set; }
    public long LastIrreversibleBlockHeight { get; set; }
    public Dictionary<string, long> Branches { get; set; } = new();
}