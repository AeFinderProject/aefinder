using System.Linq.Expressions;

namespace AElfIndexer.Sdk;

public interface IIndexerReadOnlyRepository<TEntity>
{
    Task<TEntity> GetAsync(string id);
    Task<IQueryable<TEntity>> GetQueryableAsync();
}