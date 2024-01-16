using System.Collections.Concurrent;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;

namespace AeFinder.Client.Providers;

public class DAppDataIndexProvider<TEntity> : IDAppDataIndexProvider<TEntity>
    where TEntity : AeFinderClientEntity<string>, IIndexBuild, new()
{
    private readonly ConcurrentQueue<IndexData<TEntity>> _indexDataQueue = new();
    private bool _isRegister = false;
    
    private readonly INESTRepository<TEntity, string> _nestRepository;
    private readonly IDAppDataIndexManagerProvider _dAppDataIndexManagerProvider;
    private readonly ILogger<BlockStateSetProvider<TEntity>> _logger;

    public DAppDataIndexProvider(INESTRepository<TEntity, string> nestRepository,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider, ILogger<BlockStateSetProvider<TEntity>> logger)
    {
        _nestRepository = nestRepository;
        _dAppDataIndexManagerProvider = dAppDataIndexManagerProvider;
        _logger = logger;
    }

    public async Task SaveDataAsync()
    {
        _logger.LogDebug("Saving dapp index.");
        
        var indexName= string.Empty;
        IndexOperationType operationType = IndexOperationType.AddOrUpdate;
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
            OperationType = IndexOperationType.AddOrUpdate,
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
            OperationType = IndexOperationType.Delete,
            Entity = entity
        });
        return Task.CompletedTask;
    }

    private async Task SaveIndexAsync(string indexName, IndexOperationType operationType, List<TEntity> toCommitData)
    {
        switch (operationType)
        {
            case IndexOperationType.AddOrUpdate:
                await _nestRepository.BulkAddOrUpdateAsync(toCommitData, indexName);
                toCommitData.Clear();
                break;
            case IndexOperationType.Delete:
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

        _dAppDataIndexManagerProvider.Register(this);
        _isRegister = true;
    }
}

public class IndexData<TEntity>
{
    public string IndexName { get; set; }
    public IndexOperationType OperationType { get; set; }
    public TEntity Entity { get; set; }
}

public enum IndexOperationType
{
    AddOrUpdate,
    Delete
}