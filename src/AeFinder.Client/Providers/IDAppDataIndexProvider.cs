namespace AeFinder.Client.Providers;

public interface IDAppDataIndexProvider
{
    Task SaveDataAsync();
}

public interface IDAppDataIndexProvider<TEntity> : IDAppDataIndexProvider
{
    Task AddOrUpdateAsync(TEntity entity, string indexName);
    Task DeleteAsync(TEntity entity, string indexName);
}

