
namespace AElfIndexer.Client.Providers;

public interface IDAppDataProvider
{
    Task<T> GetLibValueAsync<T>(string key);
    Task SetLibValueAsync<T>(string key, T value);
    Task SetLibValueAsync(string key, string value);
    Task SaveDataAsync();
}
