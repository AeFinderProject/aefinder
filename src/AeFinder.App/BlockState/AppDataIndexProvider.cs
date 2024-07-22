using System.Collections.Concurrent;
using AElf.EntityMapping.Repositories;
using AeFinder.Sdk.Entities;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace AeFinder.App.BlockState;

public class AppDataIndexProvider<TEntity> : IAppDataIndexProvider<TEntity>
    where TEntity : AeFinderEntity, new()
{
    private readonly ConcurrentDictionary<string, IndexData<TEntity>> _indexData = new();
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
        if (_indexData.Count <= 0)
        {
            return;
        }
        
        var indexName = _indexData.First().Value.IndexName;

        _logger.LogTrace("Saving app index. IndexName: {indexName}", indexName);
        var addOrUpdateData = new List<TEntity>();
        var deleteDate = new  List<TEntity>();
        foreach (var indexData in _indexData.Values)
        {
            switch (indexData.OperationType)
            {
                case DataOperationType.AddOrUpdate:
                    addOrUpdateData.Add(indexData.Entity);
                    break;
                case DataOperationType.Delete:
                    deleteDate.Add(indexData.Entity);
                    break;
            }
        }
        
        var tasks = new List<Task>();
        if (addOrUpdateData.Count > 0)
        {
            tasks.Add(_entityMappingRepository.AddOrUpdateManyAsync(addOrUpdateData, indexName));
        }

        if (deleteDate.Count > 0)
        {
            tasks.Add(_entityMappingRepository.DeleteManyAsync(deleteDate, indexName));
        }

        await tasks.WhenAll();
        
        _indexData.Clear();
        _logger.LogTrace("Saved app index. IndexName: {indexName}, Count: {count}", indexName,
            addOrUpdateData.Count + deleteDate.Count);
    }

    public Task AddOrUpdateAsync(TEntity entity, string indexName)
    {
        Register();

        _indexData[entity.Id] = new IndexData<TEntity>
        {
            IndexName = indexName,
            OperationType = DataOperationType.AddOrUpdate,
            Entity = entity
        };
        
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, string indexName)
    {
        Register();
        
        _indexData[entity.Id] = new IndexData<TEntity>
        {
            IndexName = indexName,
            OperationType = DataOperationType.Delete,
            Entity = entity
        };
        
        return Task.CompletedTask;
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