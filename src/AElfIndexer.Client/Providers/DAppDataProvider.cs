using System.Collections.Concurrent;
using AElfIndexer.Grains.Grain.Client;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

public class DAppDataProvider : IDAppDataProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ClientOptions _clientOptions;

    private readonly ConcurrentDictionary<string, string> _libValues = new();
    private readonly ConcurrentDictionary<string, string> _toCommitLibValues = new();
    private readonly ConcurrentQueue<string> _libValueKeys = new();

    public DAppDataProvider(IClusterClient clusterClient,IOptionsSnapshot<ClientOptions> clientOptions)
    {
        _clusterClient = clusterClient;
        _clientOptions = clientOptions.Value;
    }

    public async Task<T> GetLibValueAsync<T>(string key)
    {
        if (!_libValues.TryGetValue(key, out var value))
        {
            var dataGrain = _clusterClient.GetGrain<IDappDataGrain>(key);
            value = await dataGrain.GetLIBValue();
            SetLibValueCache(key, value);
        }
        
        return value != null ? JsonConvert.DeserializeObject<T>(value) : default;
    }

    public Task SetLibValueAsync<T>(string key, T value)
    {
        var jsonValue = JsonConvert.SerializeObject(value);
        _toCommitLibValues[key] = jsonValue;
        SetLibValueCache(key, jsonValue);
        return Task.CompletedTask;
    }
    
    public Task SetLibValueAsync(string key, string value)
    {
        _toCommitLibValues[key] = value;
        SetLibValueCache(key, value);
        return Task.CompletedTask;
    }

    private void SetLibValueCache(string key, string value)
    {
        if (_libValues.Count >= _clientOptions.DAppDataCacheCount)
        {
            if (_libValueKeys.TryPeek(out var oldKey))
            {
                _libValues.TryRemove(oldKey, out _);
                _libValueKeys.TryDequeue(out _);
            }
        }
        
        _libValueKeys.Enqueue(key);
        _libValues[key] = value;
    }

    public async Task SaveDataAsync()
    {
        var tasks = _toCommitLibValues.Select(async o =>
        {
            var dataGrain = _clusterClient.GetGrain<IDappDataGrain>(o.Key);
            await dataGrain.SetLIBValue(_libValues[o.Key]);
        });
        await tasks.WhenAll();
        _toCommitLibValues.Clear();
    }
    
    
}