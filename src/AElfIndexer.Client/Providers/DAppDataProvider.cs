using System.Collections.Concurrent;
using AElfIndexer.Grains.Grain.Client;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

internal class DAppDataProvider : IDAppDataProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    private readonly ConcurrentDictionary<string, string> _libValues = new();
    private readonly ConcurrentDictionary<string, string> _toCommitLibValues = new();

    public DAppDataProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<T> GetLibValueAsync<T>(string key)
    {
        if (!_libValues.TryGetValue(key, out var value))
        {
            var dataGrain = _clusterClient.GetGrain<IDappDataGrain>(key);
            value = await dataGrain.GetLIBValue();
            _libValues[key] = value;
        }
        
        return value != null ? JsonConvert.DeserializeObject<T>(value) : default;
    }

    public Task SetLibValueAsync<T>(string key, T value)
    {
        var jsonValue = JsonConvert.SerializeObject(value);
        _toCommitLibValues[key] = jsonValue;
        _libValues[key] = jsonValue;
        return Task.CompletedTask;
    }
    
    public Task SetLibValueAsync(string key, string value)
    {
        _toCommitLibValues[key] = value;
        _libValues[key] = value;
        return Task.CompletedTask;
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