namespace AeFinder.BlockScan;

public class AllSubscriptionDto
{
    public SubscriptionDetailDto CurrentVersion { get; set; }
    
    public SubscriptionDetailDto PendingVersion { get; set; }
}

public class SubscriptionDetailDto
{
    public string Version { get; set; }
    public SubscriptionStatus Status { get; set; }
    public SubscriptionManifestDto SubscriptionManifest { get; set; }
}