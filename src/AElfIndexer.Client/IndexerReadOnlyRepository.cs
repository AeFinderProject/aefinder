using AElf.EntityMapping.Repositories;
using AElfIndexer.Sdk;

namespace AElfIndexer.Client;

public class IndexerReadOnlyRepository<TEntity> : IIndexerReadOnlyRepository<TEntity> 
    where TEntity : IndexerEntity
{
    private readonly IEntityMappingRepository<TEntity, string> _repository;

    public IndexerReadOnlyRepository(IEntityMappingRepository<TEntity, string> repository)
    {
        _repository = repository;
    }

    public async Task<TEntity> GetAsync(string id)
    {
        return await _repository.GetAsync(id);
    }

    public async Task<IQueryable<TEntity>> GetQueryableAsync()
    {
        return await _repository.GetQueryableAsync();
    }
}