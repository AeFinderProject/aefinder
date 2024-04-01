using System.Collections.Concurrent;
using AElf.EntityMapping.Repositories;
using AeFinder.Sdk.Entities;
using Microsoft.Extensions.Logging;

namespace AeFinder.App.BlockState;

public class AppDataIndexProvider<TEntity> : IAppDataIndexProvider<TEntity>
    where TEntity : AeFinderEntity, new()
{
    private readonly ConcurrentQueue<IndexData<TEntity>> _indexDataQueue = new();
    private bool _isRegister = false;
    
    private readonly IEntityMappingRepository<TEntity, string> _entityMappingRepository;

    private readonly IAppDataIndexManagerProvider _appDataIndexManagerProvider;
    private readonly ILogger<AppDataIndexProvider<TEntity>> _logger;

    public AppDataIndexProvider(IAppDataIndexManagerProvider appDataIndexManagerProvider, ILogger<AppDataIndexProvider<TEntity>> logger, IEntityMappingRepository<TEntity, string> entityMappingRepository)
    {
        _appDataIndexManagerProvider = appDataIndexManagerProvider;
        _logger = logger;
        _entityMappingRepository = entityMappingRepository;
    }

    public async Task SaveDataAsync()
    {
        var indexName= string.Empty;
        DataOperationType operationType = DataOperationType.AddOrUpdate;
        var toCommitData = new List<TEntity>();

        while (_indexDataQueue.TryDequeue(out var data))
        {
            if (toCommitData.Count > 0 && (indexName != data.IndexName || operationType != data.OperationType))
            {
                await SaveIndexAsync(indexName,operationType, toCommitData);
            }
            
            indexName = data.IndexName;
            operationType = data.OperationType;
            toCommitData.Add(data.Entity);
        }
        
        if (toCommitData.Count > 0)
        {
            await SaveIndexAsync(indexName,operationType, toCommitData);
        }
    }

    public Task AddOrUpdateAsync(TEntity entity, string indexName)
    {
        Register();

        _indexDataQueue.Enqueue(new IndexData<TEntity>
        {
            IndexName = indexName,
            OperationType = DataOperationType.AddOrUpdate,
            Entity = entity
        });
        
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, string indexName)
    {
        Register();
        
        _indexDataQueue.Enqueue(new IndexData<TEntity>
        {
            IndexName = indexName,
            OperationType = DataOperationType.Delete,
            Entity = entity
        });
        return Task.CompletedTask;
    }

    private async Task SaveIndexAsync(string indexName, DataOperationType operationType, List<TEntity> toCommitData)
    {
        _logger.LogDebug("Saving app index. Index name: {indexName}, OperationType: {operationType}", indexName,
            operationType);
        switch (operationType)
        {
            case DataOperationType.AddOrUpdate:
                await _entityMappingRepository.AddOrUpdateManyAsync(toCommitData, indexName);
                toCommitData.Clear();
                break;
            case DataOperationType.Delete:
                await _entityMappingRepository.DeleteManyAsync(toCommitData, indexName);
                toCommitData.Clear();
                break;
        }

        _logger.LogDebug("Saved app index. Index name: {indexName}, OperationType: {operationType}", indexName,
            operationType);
    }

    private void Register()
    {
        if (_isRegister)
        {
            return;
        }

        _appDataIndexManagerProvider.Register(this);
        _isRegister = true;
    }
}

public class IndexData<TEntity>
{
    public string IndexName { get; set; }
    public DataOperationType OperationType { get; set; }
    public TEntity Entity { get; set; }
}

public enum DataOperationType
{
    AddOrUpdate,
    Delete
}