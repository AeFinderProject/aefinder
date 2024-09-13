using System.Collections.Concurrent;
using AeFinder.Apps;

namespace AeFinder.Grains.State.Subscriptions;

public class AppSubscriptionProcessingStatusState
{
    public ConcurrentDictionary<string, ProcessingStatus> ProcessingStatus { get; set; }
}