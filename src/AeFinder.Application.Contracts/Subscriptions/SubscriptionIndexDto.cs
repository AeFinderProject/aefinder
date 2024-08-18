using AeFinder.BlockScan;

namespace AeFinder.Subscriptions;

public class SubscriptionIndexDto
{
    public string AppId { get; set; }
    public string Version { get; set; }
    public SubscriptionStatus SubscriptionStatus { get; set; }
    public SubscriptionManifestDto SubscriptionManifest { get; set; }
}