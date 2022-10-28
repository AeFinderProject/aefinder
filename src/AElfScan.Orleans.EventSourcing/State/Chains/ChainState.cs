namespace AElfScan.Orleans.EventSourcing.State.Chains;

public class ChainState
{
    public long BlockHeight { get; set; }
    public string BlockHash { get; set; }
    public long ConfirmedBlockHeight { get; set; }
    public string ConfirmedBlockHash { get; set; }
}