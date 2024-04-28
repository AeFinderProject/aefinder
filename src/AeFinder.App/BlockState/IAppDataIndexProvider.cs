namespace AeFinder.App.BlockState;

public interface IAppDataIndexProvider
{
    Task SaveDataAsync();
}

public interface IAppDataIndexProvider<TEntity> : IAppDataIndexProvider
{
    Task AddOrUpdateAsync(TEntity entity, string indexName);
    Task DeleteAsync(TEntity entity, string indexName);
}

