using AElfIndexer.BlockScan;
using AElfIndexer.Grains.State.Subscriptions;

namespace AElfIndexer.Grains.State.BlockScanExecution;

public class BlockScanState
{
    public ScanInfo ScanInfo { get; set; } = new();
    public SubscriptionItem SubscriptionItem {get;set;}= new();
    public Guid MessageStreamId { get; set; }
    public ScanMode ScanMode { get; set; }
    public long ScanNewBlockStartHeight {get;set;}
    public DateTime LastHandleHistoricalBlockTime { get; set; }
}