using System.Collections.Generic;

namespace AeFinder.BlockScan;

public class SubscriptionInfoDto
{
    public SubscriptionInfoDetailDto CurrentVersion { get; set; }
    
    public SubscriptionInfoDetailDto NewVersion { get; set; }
}

public class SubscriptionInfoDetailDto
{
    public string Version { get; set; }
    public List<SubscriptionInfo> SubscriptionInfos { get; set; }
}