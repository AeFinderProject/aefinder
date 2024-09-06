using System.Collections.Concurrent;
using AeFinder.Apps;
using AeFinder.Grains.Grain.Subscriptions;

namespace AeFinder.Grains.State.Subscriptions;

public class AppSubscriptionState
{
    public string CurrentVersion { get;set; }
    public string PendingVersion { get; set; }
    public Dictionary<string, SubscriptionInfo> SubscriptionInfos { get; set; } = new();
}

public class SubscriptionInfo
{
    public SubscriptionManifest SubscriptionManifest { get; set; } 
    public SubscriptionStatus Status { get; set; }
    public ConcurrentDictionary<string, ProcessingStatus> ProcessingStatus { get; set; }
}