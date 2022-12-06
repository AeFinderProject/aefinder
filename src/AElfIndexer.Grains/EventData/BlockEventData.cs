namespace AElfIndexer.Grains.EventData;

[Serializable]
public class BlockEventData:AElfIndexer.Entities.Es.BlockBase
{
    public long LibBlockNumber { get; set; }
    
    public bool ClearBlockStateDictionary { get; set; }
}