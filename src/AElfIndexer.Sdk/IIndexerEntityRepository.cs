using System.Linq.Expressions;

namespace AElfIndexer.Sdk;

public interface IIndexerEntityRepository<TEntity>
{
    Task AddOrUpdateAsync(TEntity entity);
    Task<TEntity> GetAsync(string id);
    Task<IQueryable<TEntity>> GetQueryableAsync();
}