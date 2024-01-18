
namespace AElfIndexer.Client.Providers;

public interface IAppStateProvider
{
    Task<T> GetLastIrreversibleStateAsync<T>(string chainId, string key);
    Task SetLastIrreversibleStateAsync<T>(string chainId, string key, T value);
    Task SetLastIrreversibleStateAsync(string chainId, string key, string value);
    Task SaveDataAsync();
}
