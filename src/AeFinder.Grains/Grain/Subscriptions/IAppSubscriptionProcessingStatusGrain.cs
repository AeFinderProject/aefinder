using System.Collections.Concurrent;
using AeFinder.Apps;
using AeFinder.Grains.State.Subscriptions;

namespace AeFinder.Grains.Grain.Subscriptions;

public interface IAppSubscriptionProcessingStatusGrain : IGrainWithStringKey
{
    Task ReSetProcessingStatusAsync(List<string> chainIds);
    Task SetProcessingStatusAsync(string chainId, ProcessingStatus processingStatus);
    Task<ConcurrentDictionary<string, ProcessingStatus>> GetProcessingStatusAsync();
    Task ClearGrainStateAsync();
}