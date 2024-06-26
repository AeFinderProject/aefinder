using AeFinder.App.BlockState;
using AeFinder.Sdk.Entities;
using AElf.EntityMapping.Repositories;

namespace AeFinder.App.TestBase;

public class AppDataIndexProvider<TEntity> : IAppDataIndexProvider<TEntity>
    where TEntity : AeFinderEntity, new()
{
    private readonly IEntityMappingRepository<TEntity, string> _entityMappingRepository;
    
    public AppDataIndexProvider(IEntityMappingRepository<TEntity, string> entityMappingRepository)
    {
        _entityMappingRepository = entityMappingRepository;
    }

    public Task SaveDataAsync()
    {
        return Task.CompletedTask;
    }

    public async Task AddOrUpdateAsync(TEntity entity, string indexName)
    {
        await _entityMappingRepository.AddOrUpdateAsync(entity, indexName);
    }

    public async Task DeleteAsync(TEntity entity, string indexName)
    {
        await _entityMappingRepository.DeleteAsync(entity, indexName);
    }
}