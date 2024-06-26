using AeFinder.Grains.Grain.Subscriptions;

namespace AeFinder.Grains.State.BlockPush;

public class BlockPusherState
{
    public long PushedBlockHeight { get; set; }
    public string PushedBlockHash { get; set; }
    public long PushedConfirmedBlockHeight { get; set; }
    public string PushedConfirmedBlockHash { get; set; }
    public SortedDictionary<long, HashSet<string>> PushedBlocks = new();
    public string PushToken { get; set; }
    public string AppId { get; set; }
    public string Version { get; set; }
    public Subscription Subscription { get; set; }
}