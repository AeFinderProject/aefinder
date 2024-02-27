using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.BlockState;

public interface IAppDataIndexManagerProvider
{
    void Register(IAppDataIndexProvider provider);

    Task SavaDataAsync();
}

public class AppDataIndexManagerProvider : IAppDataIndexManagerProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<IAppDataIndexProvider, string> _dAppDataProviders = new();
    
    public void Register(IAppDataIndexProvider provider)
    {
        _dAppDataProviders.TryAdd(provider, string.Empty);
    }

    public async Task SavaDataAsync()
    {
        foreach (var provider in _dAppDataProviders.Keys)
        {
            await provider.SaveDataAsync();
        }
    }
}