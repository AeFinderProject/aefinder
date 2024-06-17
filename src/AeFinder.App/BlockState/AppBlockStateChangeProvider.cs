using System.Collections.Concurrent;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.State.BlockStates;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockState;

public class AppBlockStateChangeProvider : IAppBlockStateChangeProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, BlockStateChange>> _blockStateChanges =
        new();
    
    private readonly IClusterClient _clusterClient;
    private readonly IAppInfoProvider _appInfoProvider;

    public AppBlockStateChangeProvider(IClusterClient clusterClient, IAppInfoProvider appInfoProvider)
    {
        _clusterClient = clusterClient;
        _appInfoProvider = appInfoProvider;
    }

    public async Task SetChangeKeysAsync(string chainId, Dictionary<long,HashSet<string>> changeKeys)
    {
        var blockStateChanges = new List<BlockStateChange>();
        foreach (var changeKey in changeKeys)
        {
            var stateChange = await SetChangeKeyAsync(chainId, changeKey.Key, changeKey.Value);
            blockStateChanges.Add(stateChange);
        }

        var tasks = blockStateChanges.Select(o =>
        {
            var grain = _clusterClient.GetGrain<IAppBlockStateChangeGrain>(
                GetBlockStateChangeKey(chainId, o.BlockHeight));
            return grain.SetAsync(o);
        });
        await Task.WhenAll(tasks);
    }
    
    private async Task<BlockStateChange> SetChangeKeyAsync(string chainId, long blockHeight, HashSet<string> keys)
    {
        if(!_blockStateChanges.TryGetValue(chainId, out var changes))
        {
            changes = new ConcurrentDictionary<long, BlockStateChange>();
            _blockStateChanges[chainId] = changes;
        }

        if (!changes.TryGetValue(blockHeight, out var blockChange))
        {
            var stateChange = await GetBlockStateChangeAsync(chainId, blockHeight);
            blockChange = stateChange ?? new BlockStateChange { BlockHeight = blockHeight, ChangeKeys = new HashSet<string>() };
            _blockStateChanges[chainId][blockHeight] = blockChange;
        }

        foreach (var key in keys)
        {
            blockChange.ChangeKeys.Add(key);
        }

        return blockChange;
    }

    public async Task<HashSet<string>> GetChangeKeysAsync(string chainId, long blockHeight)
    {
        if(!_blockStateChanges.TryGetValue(chainId, out var changes))
        {
            changes = new ConcurrentDictionary<long, BlockStateChange>();
            _blockStateChanges[chainId] = changes;
        }
        
        if(!changes.TryGetValue(blockHeight, out var blockChange))
        {
            blockChange = await GetBlockStateChangeAsync(chainId, blockHeight);
            if (blockChange == null)
            {
                return null;
            }

            _blockStateChanges[chainId][blockHeight] = blockChange;
        }

        return blockChange.ChangeKeys;
    }

    public async Task CleanAsync(string chainId, long libHeight)
    {
        var toDelete = _blockStateChanges[chainId].RemoveAll(o => o.Key <= libHeight).ToList();
        
        var tasks = toDelete.Select(o =>
        {
            var grain = _clusterClient.GetGrain<IAppBlockStateChangeGrain>(
                GetBlockStateChangeKey(chainId, o.Key));
            return grain.RemoveAsync();
        });
        await Task.WhenAll(tasks);
    }

    private async Task<BlockStateChange> GetBlockStateChangeAsync(string chainId, long blockHeight)
    {
        var grain = _clusterClient.GetGrain<IAppBlockStateChangeGrain>(GetBlockStateChangeKey(chainId, blockHeight));
        return await grain.GetAsync();
    }

    private string GetBlockStateChangeKey(string chainId, long blockHeight)
    {
        return GrainIdHelper.GenerateAppBlockStateChangeGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
            chainId, blockHeight);
    }
}