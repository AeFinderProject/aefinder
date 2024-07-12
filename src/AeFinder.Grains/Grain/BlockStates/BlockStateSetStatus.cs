namespace AeFinder.Grains.Grain.BlockStates;

[GenerateSerializer]
public class BlockStateSetStatus
{
    [Id(0)]public string LongestChainBlockHash { get; set; }
    [Id(1)]public long LongestChainHeight { get; set; }
    [Id(2)]public string BestChainBlockHash { get; set; }
    [Id(3)]public long BestChainHeight { get; set; }
    [Id(4)]public string LastIrreversibleBlockHash { get; set; }
    [Id(5)]public long LastIrreversibleBlockHeight { get; set; }
    [Id(6)]public Dictionary<string, long> Branches { get; set; } = new();
}