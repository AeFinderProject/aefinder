namespace AElfIndexer.Client.BlockState;

public class NewIrreversibleBlockFoundEventData
{
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
}