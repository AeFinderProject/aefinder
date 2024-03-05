namespace AElfIndexer.Sdk;

public interface IEntityRepository<TEntity>
{
    Task<TEntity> GetAsync(string id);
    Task AddOrUpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
}