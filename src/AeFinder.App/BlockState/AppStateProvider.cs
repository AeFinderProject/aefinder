using System.Collections.Concurrent;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.State.BlockStates;
using AeFinder.Sdk;
using AeFinder.Sdk.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockState;

public class AppStateProvider : IAppStateProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly AppStateOptions _appStateOptions;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IRuntimeTypeProvider _runtimeTypeProvider;

    private readonly ConcurrentDictionary<string, AppState> _libValues = new();
    private readonly ConcurrentDictionary<string, AppState> _toCommitLibValues = new();
    private readonly ConcurrentQueue<string> _libValueKeys = new();
    
    private readonly ILogger<AppStateProvider> _logger;

    public AppStateProvider(IClusterClient clusterClient, IOptionsSnapshot<AppStateOptions> appStateOptions,
        ILogger<AppStateProvider> logger, IAppInfoProvider appInfoProvider,
        IAppBlockStateSetProvider appBlockStateSetProvider, IRuntimeTypeProvider runtimeTypeProvider)
    {
        _clusterClient = clusterClient;
        _logger = logger;
        _appInfoProvider = appInfoProvider;
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _runtimeTypeProvider = runtimeTypeProvider;
        _appStateOptions = appStateOptions.Value;
    }

    public async Task<T> GetLastIrreversibleStateAsync<T>(string chainId, string key)
    {
        var state = await GetLibStateAsync(chainId, key);
        return state != null ? JsonConvert.DeserializeObject<T>(state.Value) : default;
    }

    public async Task<object> GetLastIrreversibleStateAsync(string chainId, string key)
    {
        var state = await GetLibStateAsync(chainId, key);
        return state != null ? JsonConvert.DeserializeObject(state.Value, _runtimeTypeProvider.GetType(state.Type)) : null;
    }
    
    public async Task<T> GetStateAsync<T>(string chainId, string stateKey, IBlockIndex branchBlockIndex)
    {
        var state = await GetAppStateAsync(chainId, stateKey, branchBlockIndex);
        return state != null ? JsonConvert.DeserializeObject<T>(state.Value) : default;
    }
    
    public async Task<object> GetStateAsync(string chainId, string stateKey, IBlockIndex branchBlockIndex)
    {
        var appState = await GetAppStateAsync(chainId, stateKey, branchBlockIndex);
        return appState != null ? JsonConvert.DeserializeObject(appState.Value, _runtimeTypeProvider.GetType(appState.Type)) : null;
    }
    
    private async Task<AppState> GetAppStateAsync(string chainId, string stateKey,
        IBlockIndex branchBlockIndex)
    {
        var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId,branchBlockIndex.BlockHash);
        while (blockStateSet != null)
        {
            if (blockStateSet.Changes.TryGetValue(stateKey, out var state))
            {
                var entity = JsonConvert.DeserializeObject<EmptyAeFinderEntity>(state.Value);
                return (entity?.Metadata.IsDeleted ?? true) ? null : state;
            }

            blockStateSet =
                await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, blockStateSet.Block.PreviousBlockHash);
        }

        // if block state sets don't contain entity, return LIB value
        // lib value's block height should less than min block state set's block height.
        var lastIrreversibleState = await GetLibStateAsync(chainId, stateKey);
        if (lastIrreversibleState == null)
        {
            return null;
        }

        var lastIrreversibleStateEntity = JsonConvert.DeserializeObject<AeFinderEntity>(lastIrreversibleState.Value);
        return lastIrreversibleStateEntity != null &&
               lastIrreversibleStateEntity.Metadata.Block.BlockHeight <= branchBlockIndex.BlockHeight &&
               !lastIrreversibleStateEntity.Metadata.IsDeleted
            ? lastIrreversibleState
            : null;
    }

    private async Task<AppState> GetLibStateAsync(string chainId, string key)
    {
        var stateKey = GetAppDataKey(chainId, key);
        if (!_toCommitLibValues.TryGetValue(stateKey, out var value))
        {
            if (!_libValues.TryGetValue(stateKey, out value))
            {
                var dataGrain = _clusterClient.GetGrain<IAppStateGrain>(stateKey);
                value = await dataGrain.GetLastIrreversibleStateAsync();
                SetLibValueCache(stateKey, value, true);
            }
        }

        return value;
    }

    public Task SetLastIrreversibleStateAsync(string chainId, string key, Grains.State.BlockStates.AppState state)
    {
        var stateKey = GetAppDataKey(chainId, key);
        _toCommitLibValues[stateKey] = state;
        SetLibValueCache(stateKey, state);
        return Task.CompletedTask;
    }

    public async Task MergeStateAsync(string chainId, List<BlockStateSet> blockStateSets)
    {
        foreach (var set in blockStateSets)
        {
            foreach (var change in set.Changes)
            {
                await SetLastIrreversibleStateAsync(chainId, change.Key, change.Value);
            }
        }

        await SaveDataAsync();
    }

    private async Task SaveDataAsync()
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
    
    private void SetLibValueCache(string key, Grains.State.BlockStates.AppState state, bool isForce = false)
    {
        if (state == null)
        {
            return;
        }

        if (!isForce && !_libValues.ContainsKey(key))
        {
            return;
        }
        
        if (_libValues.Count >= _appStateOptions.AppDataCacheCount)
        {
            if (_libValueKeys.TryPeek(out var oldKey))
            {
                _libValues.TryRemove(oldKey, out _);
                _libValueKeys.TryDequeue(out _);
            }
        }
        
        _libValueKeys.Enqueue(key);
        _libValues[key] = state;
    }
    
    private string GetAppDataKey(string chainId, string key)
    {
        return GrainIdHelper.GenerateAppStateGrainId(_appInfoProvider.AppId, _appInfoProvider.Version, chainId, key);
    }
}