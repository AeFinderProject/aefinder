namespace AElfIndexer.Grains.State.Client;

public class BlockStateSet<T>
{
    public string BlockHash { get; set; }
    public string PreviousBlockHash { get; set; }
    public long BlockHeight { get; set; }
    public Dictionary<string, string> Changes { get; set; } = new ();
    
    public bool Confirmed { get; set; }
    
    public bool Processed { get; set; }
    
    public List<T> Data { get; set; } = new();
}