namespace AElfIndexer.Grains.Grain.Subscriptions;

public class AllSubscription
{
    public SubscriptionDetail CurrentVersion { get; set; }
    public SubscriptionDetail NewVersion { get; set; }
}

public class SubscriptionDetail
{
    public string Version { get; set; }
    public SubscriptionStatus Status { get; set; }
    public SubscriptionManifest SubscriptionManifest { get; set; }
}