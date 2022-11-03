namespace AElfScan.Grains.EventData;

[Serializable]
public class BlockEventData:AElfScan.AElf.Entities.Es.BlockBase
{
    public long LibBlockNumber { get; set; }
}