using AElfIndexer.Sdk.Logging;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Sdk;

public abstract class BlockDataProcessorBase : IBlockDataProcessor
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
    
    protected IIndexerLogger Logger => LazyServiceProvider.LazyGetService<IIndexerLogger>();

    protected async Task<TEntity> GetEntityAsync<TEntity>(string id)
        where TEntity : IndexerEntity
    {
        var repository = LazyServiceProvider.GetRequiredService<IEntityRepository<TEntity>>();
        return await repository.GetAsync(id);
    }

    protected async Task SaveEntityAsync<TEntity>(TEntity entity)
        where TEntity : IndexerEntity
    {
        var repository = LazyServiceProvider.GetRequiredService<IEntityRepository<TEntity>>();
        await repository.AddOrUpdateAsync(entity);
    }

    protected async Task DeleteEntityAsync<TEntity>(string id)
        where TEntity : IndexerEntity
    {
        var repository = LazyServiceProvider.GetRequiredService<IEntityRepository<TEntity>>();
        await repository.DeleteAsync(id);
    }

    protected async Task DeleteEntityAsync<TEntity>(TEntity entity)
        where TEntity : IndexerEntity
    {
        var repository = LazyServiceProvider.GetRequiredService<IEntityRepository<TEntity>>();
        await repository.DeleteAsync(entity);
    }
}