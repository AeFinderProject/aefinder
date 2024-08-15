using System.Collections.Generic;

namespace AeFinder.App.Es;

public class SubscriptionManifestInfo
{
    public List<SubscriptionInfo> SubscriptionItems { get; set; } = new();
}