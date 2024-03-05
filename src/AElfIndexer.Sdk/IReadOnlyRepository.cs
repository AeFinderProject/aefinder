namespace AElfIndexer.Sdk;

public interface IReadOnlyRepository<TEntity>
{
    Task<IQueryable<TEntity>> GetQueryableAsync();
}