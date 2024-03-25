namespace AeFinder.Sdk;

public interface IReadOnlyRepository<TEntity>
{
    Task<IQueryable<TEntity>> GetQueryableAsync();
}