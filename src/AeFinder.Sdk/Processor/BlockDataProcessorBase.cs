using AeFinder.Sdk.Entities;
using AeFinder.Sdk.Logging;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Sdk.Processor;

public abstract class BlockDataProcessorBase : IBlockDataProcessor
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
    
    protected IAeFinderLogger Logger => LazyServiceProvider.LazyGetService<IAeFinderLogger>();

    protected async Task<TEntity> GetEntityAsync<TEntity>(string id)
        where TEntity : AeFinderEntity
    {
        var repository = LazyServiceProvider.GetRequiredService<IRepository<TEntity>>();
        return await repository.GetAsync(id);
    }

    protected async Task SaveEntityAsync<TEntity>(TEntity entity)
        where TEntity : AeFinderEntity
    {
        var repository = LazyServiceProvider.GetRequiredService<IRepository<TEntity>>();
        await repository.AddOrUpdateAsync(entity);
    }

    protected async Task DeleteEntityAsync<TEntity>(string id)
        where TEntity : AeFinderEntity
    {
        var repository = LazyServiceProvider.GetRequiredService<IRepository<TEntity>>();
        await repository.DeleteAsync(id);
    }

    protected async Task DeleteEntityAsync<TEntity>(TEntity entity)
        where TEntity : AeFinderEntity
    {
        var repository = LazyServiceProvider.GetRequiredService<IRepository<TEntity>>();
        await repository.DeleteAsync(entity);
    }
}