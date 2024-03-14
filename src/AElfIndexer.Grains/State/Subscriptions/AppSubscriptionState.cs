using AElfIndexer.Grains.Grain.Subscriptions;

namespace AElfIndexer.Grains.State.Subscriptions;

public class AppSubscriptionState
{
    public string CurrentVersion { get;set; }
    public string NewVersion { get; set; }
    public Dictionary<string, SubscriptionInfo> SubscriptionInfos { get; set; } = new();
}

public class SubscriptionInfo
{
    public SubscriptionManifest SubscriptionManifest { get; set; } 
    public SubscriptionStatus Status { get; set; }
}