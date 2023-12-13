using System.Collections.Generic;

namespace AElfIndexer.BlockScan;

public class SubscriptionInfoDto
{
    public SubscriptionInfoDetailDto CurrentVersion { get; set; }
    
    public SubscriptionInfoDetailDto NewVersion { get; set; }
}

public class SubscriptionInfoDetailDto
{
    public string Version { get; set; }
    public Subscription SubscriptionInfos { get; set; }
}