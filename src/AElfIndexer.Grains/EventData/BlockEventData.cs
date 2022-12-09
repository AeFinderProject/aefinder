using AElfIndexer.Entities.Es;

namespace AElfIndexer.Grains.EventData;


// [Serializable]
public class BlockEventData:AElfIndexer.Entities.Es.BlockBase
{
    public long LibBlockHeight { get; set; }
    
    public List<Transaction> Transactions {get;set;}
    public bool ClearBlockStateDictionary { get; set; }
}