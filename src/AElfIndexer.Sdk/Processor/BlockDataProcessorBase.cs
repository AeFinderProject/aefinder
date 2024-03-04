using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Sdk;

public abstract class BlockDataProcessorBase: IBlockDataProcessor
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }
    
    private BlockDataProcessingContext _context;
    
    protected async Task<TEntity> GetEntityAsync<TEntity>(string id)
        where TEntity : IndexerEntity
    {
        var provider = LazyServiceProvider.GetRequiredService<IEntityRepository<TEntity>>();
        return await provider.GetAsync(_context.ChainId, new BlockIndex(_context.BlockHash, _context.BlockHeight),
            id);
    }
    
    protected async Task SaveEntityAsync<TEntity>(TEntity entity)
        where TEntity : IndexerEntity
    {
        SetMetadata(entity);
        var provider = LazyServiceProvider.GetRequiredService<IEntityRepository<TEntity>>();
        await provider.AddOrUpdateAsync(entity, _context.IsRollback);
    }
    
    protected async Task DeleteEntityAsync<TEntity>(TEntity entity)
        where TEntity : IndexerEntity
    {
        SetMetadata(entity);
        var provider = LazyServiceProvider.GetRequiredService<IEntityRepository<TEntity>>();
        await provider.DeleteAsync(entity, _context.IsRollback);
    }

    public void SetProcessingContext(BlockDataProcessingContext context)
    {
        _context = context;
    }
    
    private void SetMetadata<TEntity>(TEntity entity)
        where TEntity : IndexerEntity
    {
        entity.Metadata.ChainId = _context.ChainId;
        entity.Metadata.Block = new BlockMetadata
        {
            BlockHash = _context.BlockHash,
            BlockHeight = _context.BlockHeight,
            BlockTime = _context.BlockTime
        };
    }
}