namespace AeFinder.Grains.State.Chains;

[GenerateSerializer]
public class ChainState
{
    [Id(0)]public long BlockHeight { get; set; }
    [Id(1)]public string BlockHash { get; set; }
    [Id(2)]public long ConfirmedBlockHeight { get; set; }
    [Id(3)]public string ConfirmedBlockHash { get; set; }
}