using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.OperationLimits;

public interface IOperationLimitManager
{
    void Add(IOperationLimitProvider provider);
    void ResetAll();
}

public class OperationLimitManager : IOperationLimitManager, ISingletonDependency
{
    private readonly List<IOperationLimitProvider> _providers = new();

    public void Add(IOperationLimitProvider provider)
    {
        _providers.Add(provider);
    }
    
    public void ResetAll()
    {
        foreach (var provider in _providers)
        {
            provider.Reset();
        }
    }
}