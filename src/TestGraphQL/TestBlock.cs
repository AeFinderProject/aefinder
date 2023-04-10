namespace GraphQL;

public class TestBlock
{
    public  string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    
    public string PreviousBlockHash { get; set; }
    
    public DateTime BlockTime { get; set; }
    
    public string SignerPubkey { get; set; }
    
    public string Signature { get; set; }
    
    public bool Confirmed{get;set;}
}