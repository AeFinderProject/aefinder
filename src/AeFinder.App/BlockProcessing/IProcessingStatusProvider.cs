using System.Collections.Concurrent;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockProcessing;

public interface IProcessingStatusProvider
{
    void SetStatus(string chainId, ProcessingStatus status);
    bool IsRunning(string chainId);
    Task SetSubscriptionProcessingStatusAsync(string appId, string version, string chainId,
        ProcessingStatus processingStatus);
}

public class ProcessingStatusProvider : IProcessingStatusProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    
    public ProcessingStatusProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    
    private readonly ConcurrentDictionary<string, ProcessingStatus> _status = new();

    public void SetStatus(string chainId, ProcessingStatus status)
    {
        _status[chainId] = status;
    }

    public bool IsRunning(string chainId)
    {
        return _status.TryGetValue(chainId, out var status) && status == ProcessingStatus.Running;
    }

    public async Task SetSubscriptionProcessingStatusAsync(string appId, string version, string chainId,
        ProcessingStatus processingStatus)
    {
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        await appSubscriptionGrain.SetProcessingStatusAsync(version, chainId, processingStatus);
    }
}

