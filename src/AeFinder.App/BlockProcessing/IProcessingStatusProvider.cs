using System.Collections.Concurrent;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Subscriptions;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockProcessing;

public interface IProcessingStatusProvider
{
    void SetStatus(string appId, string version, string chainId, ProcessingStatus status);
    bool IsRunning(string chainId);
}

public class ProcessingStatusProvider : IProcessingStatusProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    
    public ProcessingStatusProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }
    
    private readonly ConcurrentDictionary<string, ProcessingStatus> _status = new();

    public void SetStatus(string appId, string version, string chainId, ProcessingStatus status)
    {
        _status[chainId] = status;
        _ = SetAppProcessingStatusAsync(appId, version, chainId, status);
    }

    public bool IsRunning(string chainId)
    {
        return _status.TryGetValue(chainId, out var status) && status == ProcessingStatus.Running;
    }

    private async Task SetAppProcessingStatusAsync(string appId, string version, string chainId,
        ProcessingStatus processingStatus)
    {
        var appSubscriptionProcessingStatusGrain =
            _clusterClient.GetGrain<IAppSubscriptionProcessingStatusGrain>(GrainIdHelper.GenerateAppSubscriptionProcessingStatusGrainId(appId,version));
        await appSubscriptionProcessingStatusGrain.SetProcessingStatusAsync(chainId, processingStatus);
    }
}

