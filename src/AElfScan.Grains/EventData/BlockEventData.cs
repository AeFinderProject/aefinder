namespace AElfScan.Grains.EventData;

[Serializable]
public class BlockEventData:AElfScan.Entities.Es.BlockBase
{
    public long LibBlockNumber { get; set; }
    
    public bool ClearBlockStateDictionary { get; set; }
}