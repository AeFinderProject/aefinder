using AElfIndexer.Grains.Grain.Client;

namespace AElfIndexer.Client.Providers;

public interface IDappDataProvider
{
    Task<T> GetLIBValueAsync<T>(string key);
    Task SetLIBAsync<T>(string key, string value);
    Task CommitAsync();
}
