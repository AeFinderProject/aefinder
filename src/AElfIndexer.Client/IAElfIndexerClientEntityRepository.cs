using AElfIndexer.Client.Handlers;

namespace AElfIndexer.Client;

public interface IAElfIndexerClientEntityRepository<TEntity,TKey,TData,T> where TEntity: AElfIndexerClientEntity<TKey>, new() 
    where TData : BlockChainDataBase
{
    Task AddOrUpdateAsync(TEntity entity);
    Task<TEntity> GetAsync(TKey id, string chainId);
}