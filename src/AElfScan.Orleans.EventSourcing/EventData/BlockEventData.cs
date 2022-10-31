namespace AElfScan.EventData;

[Serializable]
public class BlockEventData:AElfScan.AElf.Entities.Es.BlockIndex
{
    // public string ChainId { get; set; }
    // public string BlockHash { get; set; }
    // public long BlockNumber { get; set; }
    // public string PreviousBlockHash { get; set; }
    // public DateTime BlockTime{get;set;}
    public long LibBlockNumber { get; set; }
}