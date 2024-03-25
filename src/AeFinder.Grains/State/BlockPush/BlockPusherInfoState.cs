using AeFinder.Grains.Grain.Subscriptions;

namespace AeFinder.Grains.State.BlockPush;

public class BlockPusherInfoState
{
    public BlockPushInfo BlockPushInfo { get; set; } = new();
    public Subscription Subscription {get;set;}= new();
    public Guid MessageStreamId { get; set; }
    public BlockPushMode BlockPushMode { get; set; }
    public long NewBlockStartHeight {get;set;}
    public DateTime LastHandleHistoricalBlockTime { get; set; }
}