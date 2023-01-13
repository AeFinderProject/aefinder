using System.Collections.Concurrent;
using AElfIndexer.Grains.Grain.Client;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

internal class DappDataProvider : IDappDataProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    private ConcurrentDictionary<string, string> _libValues = new();
    private ConcurrentDictionary<string, string> _toCommitLibValues = new();

    public DappDataProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<T> GetLIBValueAsync<T>(string key)
    {
        if (!_libValues.TryGetValue(key, out var value))
        {
            var dappDataGrain = _clusterClient.GetGrain<IDappDataGrain>(key);
            value = await dappDataGrain.GetLIBValue();
            _libValues[key] = value;
        }
        
        return value != null ? JsonConvert.DeserializeObject<T>(value) : default;
    }

    public async Task SetLIBAsync<T>(string key, string value)
    {
        var jsonValue = JsonConvert.SerializeObject(value);
        _toCommitLibValues[key] = jsonValue;
        _libValues[key] = jsonValue;
    }

    public async Task CommitAsync()
    {
        foreach (var value in _toCommitLibValues)
        {
            var dappDataGrain = _clusterClient.GetGrain<IDappDataGrain>(value.Key);
            await dappDataGrain.SetLIBValue(_libValues[value.Value]);
        }
        _toCommitLibValues.Clear();
    }
}