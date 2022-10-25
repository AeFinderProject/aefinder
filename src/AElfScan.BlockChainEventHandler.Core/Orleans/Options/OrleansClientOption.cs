namespace AElfScan.Options;

public class OrleansClientOption
{
    public string ClusterId { get; set; }
    public string ServiceId { get; set; }
    public int AElfBlockGrainPrimaryKey { get; set; }
    public string KVrocksConnection { get; set; }
}