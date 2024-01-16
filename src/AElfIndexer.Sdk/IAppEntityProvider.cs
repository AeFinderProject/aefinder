namespace AElfIndexer.Sdk;

public interface IAppEntityProvider<TEntity>
{
    Task<TEntity> GetAsync(string id);
    Task AddOrUpdateAsync(TEntity entity);
}