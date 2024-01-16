using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Client.Providers;

public interface IDAppDataIndexManagerProvider
{
    void Register(IDAppDataIndexProvider provider);

    Task SavaDataAsync();
}

public class DAppDataIndexManagerProvider : IDAppDataIndexManagerProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<IDAppDataIndexProvider, string> _dAppDataProviders = new();
    
    public void Register(IDAppDataIndexProvider provider)
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