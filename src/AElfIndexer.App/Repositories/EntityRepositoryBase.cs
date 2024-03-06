using AElfIndexer.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.App.Repositories;

public abstract class EntityRepositoryBase<TEntity>
    where TEntity : IndexerEntity, IIndexerEntity
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
    protected IAppInfoProvider AppInfoProvider => LazyServiceProvider.LazyGetRequiredService<IAppInfoProvider>();

    protected readonly string EntityName = typeof(TEntity).Name;
    
    protected string GetIndexName()
    {
        return $"{AppInfoProvider.AppId}-{AppInfoProvider.Version}.{EntityName}".ToLower();
    }
}