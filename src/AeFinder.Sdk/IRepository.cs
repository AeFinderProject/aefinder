namespace AeFinder.Sdk;

public interface IRepository<TEntity>
{
    Task<TEntity> GetAsync(string id);
    Task AddOrUpdateAsync(TEntity entity);
    Task DeleteAsync(string id);
    Task DeleteAsync(TEntity entity);
}