using AElf.EntityMapping.Repositories;
using AeFinder.Sdk;
using AeFinder.Sdk.Entities;

namespace AeFinder.App.Repositories;

public class ReadOnlyRepository<TEntity> : RepositoryBase<TEntity>, IReadOnlyRepository<TEntity> 
    where TEntity : AeFinderEntity, IAeFinderEntity
{
    private readonly IEntityMappingRepository<TEntity, string> _repository;
    
    public ReadOnlyRepository(IEntityMappingRepository<TEntity, string> repository)
    {
        _repository = repository;
    }

    public async Task<IQueryable<TEntity>> GetQueryableAsync()
    {
        return await _repository.GetQueryableAsync(GetIndexName());
    }
}