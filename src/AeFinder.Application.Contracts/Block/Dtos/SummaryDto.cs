namespace AeFinder.Block.Dtos;

public class SummaryDto
{
    public string ChainId { get; set; }
    public long LatestBlockHeight { get; set; }
    public long ConfirmedBlockHeight { get; set; }
}