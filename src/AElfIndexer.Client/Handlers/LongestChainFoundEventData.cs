namespace AElfIndexer.Client.Handlers;

public class LongestChainFoundEventData
{
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
}