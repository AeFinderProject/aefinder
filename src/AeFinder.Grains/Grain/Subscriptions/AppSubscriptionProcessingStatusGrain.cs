using System.Collections.Concurrent;
using AeFinder.Apps;
using AeFinder.Grains.State.Subscriptions;

namespace AeFinder.Grains.Grain.Subscriptions;

public class AppSubscriptionProcessingStatusGrain: AeFinderGrain<AppSubscriptionProcessingStatusState>, IAppSubscriptionProcessingStatusGrain
{
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        await ReadStateAsync();
    }
    
    public async Task ReSetProcessingStatusAsync(List<string> chainIds)
    {
        await ReadStateAsync();
        if (State.ProcessingStatus == null)
        {
            State.ProcessingStatus =
                new ConcurrentDictionary<string, ProcessingStatus>();
        }
        foreach (var chainId in chainIds)
        {
            State.ProcessingStatus.AddOrUpdate(chainId, ProcessingStatus.Running,
                (key, oldValue) => ProcessingStatus.Running);
        }
        await WriteStateAsync();
    }
    
    public async Task SetProcessingStatusAsync(string chainId, ProcessingStatus processingStatus)
    {
        await ReadStateAsync();
        if (State.ProcessingStatus == null)
        {
            State.ProcessingStatus =
                new ConcurrentDictionary<string, ProcessingStatus>();
        }
        State.ProcessingStatus[chainId] = processingStatus;
        await WriteStateAsync();
    }

    public async Task<ConcurrentDictionary<string, ProcessingStatus>> GetProcessingStatusAsync()
    {
        return State.ProcessingStatus;
    }
    
    public async Task ClearGrainStateAsync()
    {
        await base.ClearStateAsync();
        DeactivateOnIdle();
    }
}