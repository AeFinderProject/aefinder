using System.Collections.Concurrent;
using AElf.Indexing.Elasticsearch;

namespace AElfIndexer.Client.Providers;

public class DAppDataIndexProvider<TEntity> : IDAppDataIndexProvider<TEntity>
    where TEntity : AElfIndexerClientEntity<string>, IIndexBuild, new()
{
    private readonly ConcurrentDictionary<string, List<TEntity>> _addOrUpdateData = new();
    private readonly ConcurrentDictionary<string, List<TEntity>> _deleteData = new();
    private bool _isRegister = false;
    
    private readonly INESTRepository<TEntity, string> _nestRepository;
    private readonly IDAppDataIndexManagerProvider _dAppDataIndexManagerProvider;

    public DAppDataIndexProvider(INESTRepository<TEntity, string> nestRepository,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider)
    {
        _nestRepository = nestRepository;
        _dAppDataIndexManagerProvider = dAppDataIndexManagerProvider;
    }

    public async Task SaveDataAsync()
    {
        foreach (var data in _addOrUpdateData)
        {
            await _nestRepository.BulkAddOrUpdateAsync(data.Value, data.Key);
        }
        _addOrUpdateData.Clear();
        
        foreach (var data in _deleteData)
        {
            await _nestRepository.BulkDeleteAsync(data.Value, data.Key);
        }
        _deleteData.Clear();
    }

    public async Task AddOrUpdateAsync(TEntity entity, string indexName)
    {
        Register();
        
        if (!_addOrUpdateData.TryGetValue(indexName, out var value))
        {
            value = new List<TEntity>();
        }
        value.Add(entity);
        _addOrUpdateData[indexName] = value;
    }

    public async Task DeleteAsync(TEntity entity, string indexName)
    {
        Register();
        
        if (!_deleteData.TryGetValue(indexName, out var value))
        {
            value = new List<TEntity>();
        }
        value.Add(entity);
        _deleteData[indexName] = value;
    }

    private void Register()
    {
        if (!_isRegister)
        {
            _dAppDataIndexManagerProvider.Register(this);
        }
    }
}