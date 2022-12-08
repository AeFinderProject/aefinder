namespace AElfIndexer.Grains.State.Client;

public class BlockStateSet<T>
{
    public string BlockHash { get; set; }
    public string PreviousBlockHash { get; set; }
    public long BlockHeight { get; set; }
    public Dictionary<string, string> Changes { get; set; } = new ();
    public List<T> Data { get; set; } = new();
}