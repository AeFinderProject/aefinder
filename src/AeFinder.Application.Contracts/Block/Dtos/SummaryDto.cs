namespace AeFinder.Block.Dtos;

public class SummaryDto
{
    public string ChainId { get; set; }
    public long LatestBlockHeight { get; set; }
    public string LatestBlockHash { get; set; }
    public long ConfirmedBlockHeight { get; set; }
    public string ConfirmedBlockHash { get; set; }
}