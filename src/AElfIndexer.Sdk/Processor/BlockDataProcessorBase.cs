using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Sdk.Processor;

public abstract class BlockDataProcessorBase
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }

    protected async Task<TEntity> GetEntityAsync<TEntity>(string id)
    {
        var service = LazyServiceProvider.GetRequiredService<IDAppEntityProvider<TEntity>>();
        return await service.GetAsync(id);
    }
    
    protected async Task SaveEntityAsync<TEntity>(TEntity entity)
    {
        var service = LazyServiceProvider.GetRequiredService<IDAppEntityProvider<TEntity>>();
        await service.AddOrUpdateAsync(entity);
    }
}