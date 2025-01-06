namespace AeFinder.GraphQL.Dto;

public class SyncStateVersionItemDto
{
    public string ChainId { get; set; }
    public string LongestChainBlockHash { get; set; }
    public int LongestChainHeight { get; set; }
    public string BestChainBlockHash { get; set; }
    public int BestChainHeight { get; set; }
    public string LastIrreversibleBlockHash { get; set; }
    public int LastIrreversibleBlockHeight { get; set; }
}