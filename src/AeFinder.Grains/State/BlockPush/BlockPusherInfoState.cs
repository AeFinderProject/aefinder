using AeFinder.Grains.Grain.Subscriptions;

namespace AeFinder.Grains.State.BlockPush;

[GenerateSerializer]
public class BlockPusherInfoState
{
    [Id(0)]public BlockPushInfo BlockPushInfo { get; set; } = new();
    [Id(1)]public Subscription Subscription {get;set;}= new();
    [Id(2)]public Guid MessageStreamId { get; set; }
    [Id(3)]public BlockPushMode BlockPushMode { get; set; }
    [Id(4)]public long NewBlockStartHeight {get;set;}
    [Id(5)]public DateTime LastHandleHistoricalBlockTime { get; set; }
}