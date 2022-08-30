namespace AElfScan.EventData;

[Serializable]
public class BlockEventData
{
    public BlockEventData()
    {
        BlockTime=DateTime.UtcNow;
    }
    
    public long BlockNumber { get; set; }
    public DateTime BlockTime{get;set;}
    public bool IsConfirmed{get;set;}
}