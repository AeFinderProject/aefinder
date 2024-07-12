namespace AeFinder.Grains.Grain.Subscriptions;

[GenerateSerializer]
public class AllSubscription
{
    [Id(0)]public SubscriptionDetail CurrentVersion { get; set; }
    [Id(1)]public SubscriptionDetail PendingVersion { get; set; }
}

[GenerateSerializer]
public class SubscriptionDetail
{
    [Id(0)]public string Version { get; set; }
    [Id(1)]public SubscriptionStatus Status { get; set; }
    [Id(2)]public SubscriptionManifest SubscriptionManifest { get; set; }
}