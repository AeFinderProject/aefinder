using AElfIndexer.Grains.State.Subscriptions;

namespace AElfIndexer.Grains.Grain.ScanApps;

public class AllSubscription
{
    public SubscriptionDetail CurrentVersion { get; set; }
    public SubscriptionDetail NewVersion { get; set; }
}

public class SubscriptionDetail
{
    public string Version { get; set; }
    public SubscriptionStatus Status { get; set; }
    public Subscription Subscription { get; set; }
}