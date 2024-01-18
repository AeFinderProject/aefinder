using System.Collections.Concurrent;
using AElf.Indexing.Elasticsearch;
using AElfIndexer.Sdk;
using Microsoft.Extensions.Logging;

namespace AElfIndexer.Client.Providers;

public class AppDataIndexProvider<TEntity> : IAppDataIndexProvider<TEntity>
    where TEntity : IndexerEntity, IIndexBuild, new()
{
    private readonly ConcurrentQueue<IndexData<TEntity>> _indexDataQueue = new();
    private bool _isRegister = false;
    
    private readonly INESTRepository<TEntity, string> _nestRepository;

    private readonly IAppDataIndexManagerProvider _appDataIndexManagerProvider;
    private readonly ILogger<AppDataIndexProvider<TEntity>> _logger;

    public AppDataIndexProvider(INESTRepository<TEntity, string> nestRepository,
        IAppDataIndexManagerProvider appDataIndexManagerProvider, ILogger<AppDataIndexProvider<TEntity>> logger)
    {
        _nestRepository = nestRepository;
        _appDataIndexManagerProvider = appDataIndexManagerProvider;
        _logger = logger;
    }

    public async Task SaveDataAsync()
    {
        _logger.LogDebug("Saving dapp index.");
        
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
        
        _logger.LogDebug("Saved dapp index.");
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
        switch (operationType)
        {
            case DataOperationType.AddOrUpdate:
                await _nestRepository.BulkAddOrUpdateAsync(toCommitData, indexName);
                toCommitData.Clear();
                break;
            case DataOperationType.Delete:
                await _nestRepository.BulkDeleteAsync(toCommitData, indexName);
                toCommitData.Clear();
                break;
        }
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