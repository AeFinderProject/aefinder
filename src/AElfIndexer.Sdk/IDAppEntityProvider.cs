namespace AElfIndexer.Sdk;

public interface IDAppEntityProvider<TEntity>
{
    Task<TEntity> GetAsync(string key);
    Task AddOrUpdateAsync(TEntity entity);
}