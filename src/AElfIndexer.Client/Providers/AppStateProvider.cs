using System.Collections.Concurrent;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.BlockStates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Client.Providers;

public class AppStateProvider : IAppStateProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly ClientOptions _clientOptions;
    private readonly AppInfoOptions _appInfoOptions;

    private readonly ConcurrentDictionary<string, string> _libValues = new();
    private readonly ConcurrentDictionary<string, string> _toCommitLibValues = new();
    private readonly ConcurrentQueue<string> _libValueKeys = new();
    
    private readonly ILogger<AppStateProvider> _logger;

    public AppStateProvider(IClusterClient clusterClient, IOptionsSnapshot<ClientOptions> clientOptions,
        ILogger<AppStateProvider> logger, IOptionsSnapshot<AppInfoOptions> appInfoOptions)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _appInfoOptions = appInfoOptions.Value;
        _clientOptions = clientOptions.Value;
    }

    public async Task<T> GetLastIrreversibleStateAsync<T>(string chainId, string key)
    {
        var stateKey = GetAppDataKey(chainId, key);
        if (!_toCommitLibValues.TryGetValue(stateKey, out var value))
        {
            if (!_libValues.TryGetValue(stateKey, out value))
            {
                var dataGrain = _clusterClient.GetGrain<IAppStateGrain>(stateKey);
                value = await dataGrain.GetLastIrreversibleStateAsync();
                SetLibValueCache(stateKey, value);
            }
        }

        return value != null ? JsonConvert.DeserializeObject<T>(value) : default;
    }

    public Task SetLastIrreversibleStateAsync<T>(string chainId, string key, T value)
    {
        var stateKey = GetAppDataKey(chainId, key);
        var jsonValue = JsonConvert.SerializeObject(value);
        _toCommitLibValues[stateKey] = jsonValue;
        SetLibValueCache(stateKey, jsonValue);
        return Task.CompletedTask;
    }
    
    public Task SetLastIrreversibleStateAsync(string chainId, string key, string value)
    {
        var stateKey = GetAppDataKey(chainId, key);
        _toCommitLibValues[stateKey] = value;
        SetLibValueCache(stateKey, value);
        return Task.CompletedTask;
    }

    public async Task SaveDataAsync()
    {
        _logger.LogDebug("Saving dapp data.");
        var tasks = _toCommitLibValues.Select(async o =>
        {
            var dataGrain = _clusterClient.GetGrain<IAppStateGrain>(o.Key);
            await dataGrain.SetLastIrreversibleStateAsync(o.Value);
        });
        await tasks.WhenAll();
        _toCommitLibValues.Clear();
        _logger.LogDebug("Saved dapp data.");
    }
    
    private void SetLibValueCache(string key, string value)
    {
        if (_libValues.Count >= _clientOptions.AppDataCacheCount)
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
    
    private string GetAppDataKey(string chainId, string key)
    {
        return GrainIdHelper.GenerateAppStateGrainId(_appInfoOptions.ScanAppId, _appInfoOptions.Version, chainId, key);
    }
}