using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.App.BlockState;

public interface IAppDataIndexManagerProvider
{
    void Register(IAppDataIndexProvider provider);

    Task SavaDataAsync();
}

public class AppDataIndexManagerProvider : IAppDataIndexManagerProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<IAppDataIndexProvider, string> _appDataProviders = new();
    
    public void Register(IAppDataIndexProvider provider)
    {
        _appDataProviders.TryAdd(provider, string.Empty);
    }

    public async Task SavaDataAsync()
    {
        var tasks = _appDataProviders.Keys.Select(p => p.SaveDataAsync());
        await Task.WhenAll(tasks);
    }
}