using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockProcessing;

public interface IProcessingStatusProvider
{
    void SetStatus(string chainId, ProcessingStatus status);
    bool IsRunning(string chainId);
}

public class ProcessingStatusProvider : IProcessingStatusProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, ProcessingStatus> _status = new();

    public void SetStatus(string chainId, ProcessingStatus status)
    {
        _status[chainId] = status;
    }

    public bool IsRunning(string chainId)
    {
        return _status.TryGetValue(chainId, out var status) && status == ProcessingStatus.Running;
    }
}

public enum ProcessingStatus
{
    Running,
    Failed
}