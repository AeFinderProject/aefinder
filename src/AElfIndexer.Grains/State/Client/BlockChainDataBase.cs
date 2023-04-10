namespace AElfIndexer.Grains.State.Client;

public class BlockChainDataBase
{
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    public bool Confirmed { get; set; }
    public Dictionary<string, string> ExtraProperties { get; set; }
}