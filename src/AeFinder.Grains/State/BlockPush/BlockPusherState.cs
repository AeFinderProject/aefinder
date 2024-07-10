using AeFinder.Grains.Grain.Subscriptions;

namespace AeFinder.Grains.State.BlockPush;

[GenerateSerializer]
public class BlockPusherState
{
    [Id(0)]public long PushedBlockHeight { get; set; }
    [Id(1)]public string PushedBlockHash { get; set; }
    [Id(2)]public long PushedConfirmedBlockHeight { get; set; }
    [Id(3)]public string PushedConfirmedBlockHash { get; set; }
    [Id(4)]public SortedDictionary<long, HashSet<string>> PushedBlocks = new();
    [Id(5)]public string PushToken { get; set; }
    [Id(6)]public string AppId { get; set; }
    [Id(7)]public string Version { get; set; }
    [Id(8)]public Subscription Subscription { get; set; }
}