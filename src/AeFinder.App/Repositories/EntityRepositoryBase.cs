using AeFinder.Sdk.Entities;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.Repositories;

public abstract class EntityRepositoryBase<TEntity>
    where TEntity : AeFinderEntity, IAeFinderEntity
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
    protected IAppInfoProvider AppInfoProvider => LazyServiceProvider.LazyGetRequiredService<IAppInfoProvider>();

    protected readonly string EntityName = typeof(TEntity).Name;
    
    protected string GetIndexName()
    {
        return $"{AppInfoProvider.AppId}-{AppInfoProvider.Version}.{EntityName}".ToLower();
    }
}