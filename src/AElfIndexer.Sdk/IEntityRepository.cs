namespace AElfIndexer.Sdk;

public interface IEntityRepository<TEntity>
{
    Task<TEntity> GetAsync(string chainId, IBlockIndex blockIndex, string id);
    Task AddOrUpdateAsync(TEntity entity, bool isRollback);
}