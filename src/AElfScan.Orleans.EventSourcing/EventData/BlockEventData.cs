namespace AElfScan.EventData;

[Serializable]
public class BlockEventData
{
    public BlockEventData()
    {
    }

    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockNumber { get; set; }
    public string PreviousBlockHash { get; set; }
    public DateTime BlockTime{get;set;}
    // public bool IsConfirmed{get;set;}
    public string LibBlockHash { get; set; }
    public long LibBlockNumber { get; set; }
}