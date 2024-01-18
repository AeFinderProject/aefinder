namespace AElfIndexer.Sdk;

public interface IReadOnlyRepository<TEntity>
{
    Task<TEntity> GetAsync(string id);
    Task<IQueryable<TEntity>> GetQueryableAsync();
}