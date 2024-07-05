namespace AeFinder.Grains.State.Chains;

[GenerateSerializer]
public class ChainState
{
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
    public long ConfirmedBlockHeight { get; set; }
    public string ConfirmedBlockHash { get; set; }
}