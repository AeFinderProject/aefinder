namespace AeFinder.Grains.EventData;


// [Serializable]
public class BlockData
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime { get; set; }
    public string SignerPubkey { get; set; }
    public string Signature { get; set; }
    public bool Confirmed{get;set;}
    public Dictionary<string,string> ExtraProperties {get;set;}
    
    public long LibBlockHeight { get; set; }
    
    public List<Transaction> Transactions {get;set;}
    // public bool ClearBlockStateDictionary { get; set; }
}
