using AElf.EntityMapping.Repositories;
using AElfIndexer.Sdk;

namespace AElfIndexer.App.Repositories;

public class ReadOnlyRepository<TEntity> : IReadOnlyRepository<TEntity> 
    where TEntity : IndexerEntity
{
    private readonly IEntityMappingRepository<TEntity, string> _repository;

    public ReadOnlyRepository(IEntityMappingRepository<TEntity, string> repository)
    {
        _repository = repository;
    }
    
    public async Task<IQueryable<TEntity>> GetQueryableAsync()
    {
        return await _repository.GetQueryableAsync();
    }
}