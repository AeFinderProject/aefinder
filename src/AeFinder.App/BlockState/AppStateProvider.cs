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

    private readonly ConcurrentDictionary<string, AppStateDto> _libValues = new();
    private readonly ConcurrentDictionary<string, AppStateDto> _toCommitLibValues = new();
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

    public async Task PreMergeStateAsync(string chainId, List<BlockStateSet> blockStateSets)
    {
        var toMergeStates = new Dictionary<string, AppState>();
        foreach (var change in blockStateSets.SelectMany(set => set.Changes))
        {
            toMergeStates[change.Key] = change.Value;
        }

        var tasks = toMergeStates.Select(async o => await SetPendingStateAsync(chainId, o.Key, o.Value));
        await tasks.WhenAll();

        await SaveDataAsync(chainId);
    }
    
    public async Task MergeStateAsync(string chainId, List<BlockStateSet> blockStateSets)
    {
        var toMergeKeys = new HashSet<string>();
        foreach (var change in blockStateSets.SelectMany(set => set.Changes))
        {
            toMergeKeys.Add(change.Key);
        }
        
        var tasks = toMergeKeys.Select(async o => await MergeStateAsync(chainId, o));
        await tasks.WhenAll();

        await SaveDataAsync(chainId);
    }

    private async Task<AppState> GetAppStateAsync(string chainId, string stateKey,
        IBlockIndex branchBlockIndex)
    {
        var blockStateSet = await _appBlockStateSetProvider.GetBlockStateSetAsync(chainId, branchBlockIndex.BlockHash);
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
        if (lastIrreversibleState == null || (lastIrreversibleState.LastIrreversibleState == null &&
                                              lastIrreversibleState.PendingState == null))
        {
            return null;
        }

        if (lastIrreversibleState.PendingState != null)
        {
            var stateEntity = JsonConvert.DeserializeObject<EmptyAeFinderEntity>(lastIrreversibleState.PendingState.Value);
            if (stateEntity.Metadata.Block.BlockHeight <= branchBlockIndex.BlockHeight)
            {
                var lastIrreversibleBlockStateSet = await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId);
                if (stateEntity.Metadata.Block.BlockHeight <= lastIrreversibleBlockStateSet.Block.BlockHeight)
                {
                    await MergeStateAsync(chainId, stateKey);
                }

                return stateEntity.Metadata.IsDeleted ? null : lastIrreversibleState.PendingState;
            }
        }

        if (lastIrreversibleState.LastIrreversibleState != null)
        {
            var lastIrreversibleStateEntity =
                JsonConvert.DeserializeObject<EmptyAeFinderEntity>(lastIrreversibleState.LastIrreversibleState.Value);
            
            if (lastIrreversibleStateEntity.Metadata.Block.BlockHeight <= branchBlockIndex.BlockHeight)
            {
                return lastIrreversibleStateEntity.Metadata.IsDeleted
                    ? null
                    : lastIrreversibleState.LastIrreversibleState;
            }
        }

        return null;
    }

    private async Task<AppStateDto> GetLibStateAsync(string chainId, string key)
    {
        var stateKey = GetAppDataKey(chainId, key);
        if (!_toCommitLibValues.TryGetValue(stateKey, out var value))
        {
            if (!_libValues.TryGetValue(stateKey, out value))
            {
                var dataGrain = _clusterClient.GetGrain<IAppStateGrain>(stateKey);
                value = await dataGrain.GetStateAsync();
                SetLibValueCache(stateKey, value, true);
            }
        }

        return value;
    }
    
    private async Task SetPendingStateAsync(string chainId, string key, AppState state)
    {
        var currentState = await GetLibStateAsync(chainId, key);
        currentState.PendingState = state;
        var stateKey = GetAppDataKey(chainId, key);
        _toCommitLibValues[stateKey] = currentState;
        SetLibValueCache(stateKey, currentState);
    }

    private async Task MergeStateAsync(string chainId, string key)
    {
        var currentState = await GetLibStateAsync(chainId, key);
        currentState.LastIrreversibleState = currentState.PendingState;
        var stateKey = GetAppDataKey(chainId, key);
        _toCommitLibValues[stateKey] = currentState;
        SetLibValueCache(stateKey, currentState);
    }

    private async Task SaveDataAsync(string chainId)
    {
        _logger.LogTrace("[{ChainId}] Saving dapp data.", chainId);
        
        var groupedLibValues = _toCommitLibValues
            .Select((pair, index) => new { pair, groupIndex = index / _appStateOptions.MaxAppStateBatchCommitCount })
            .GroupBy(x => x.groupIndex, x => x.pair);
        
        _logger.LogTrace("[{ChainId}] Saving dapp data. Group count: {Count}", chainId, groupedLibValues.Count());
        
        foreach (var items in groupedLibValues)
        {
            var tasks = items.Select(async o =>
            {
                var dataGrain = _clusterClient.GetGrain<IAppStateGrain>(o.Key);
                await dataGrain.SetStateAsync(o.Value);
            });
            await tasks.WhenAll();
            
        }

        _toCommitLibValues.Clear();
        _logger.LogTrace("[{ChainId}] Saved dapp data.", chainId);
    }
    
    private void SetLibValueCache(string key, AppStateDto state, bool isForce = false)
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