using System.Collections.Concurrent;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.State.BlockStates;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockState;

public class AppBlockStateChangeProvider : IAppBlockStateChangeProvider, ISingletonDependency
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, Dictionary<string, BlockStateChange>>> _blockStateChanges =
        new();
    
    private readonly IClusterClient _clusterClient;
    private readonly IAppInfoProvider _appInfoProvider;

    public AppBlockStateChangeProvider(IClusterClient clusterClient, IAppInfoProvider appInfoProvider)
    {
        _clusterClient = clusterClient;
        _appInfoProvider = appInfoProvider;
    }

    public async Task AddBlockStateChangeAsync(string chainId, Dictionary<long,List<BlockStateChange>> changeKeys)
    {
        var blockStateChanges = new Dictionary<long,Dictionary<string, BlockStateChange>>();
        foreach (var changeKey in changeKeys)
        {
            var stateChange = await AddBlockStateChangeAsync(chainId, changeKey.Key, changeKey.Value);
            blockStateChanges[changeKey.Key] = stateChange;
        }

        var tasks = blockStateChanges.Select(o =>
        {
            var grain = _clusterClient.GetGrain<IAppBlockStateChangeGrain>(
                GetBlockStateChangeKey(chainId, o.Key));
            return grain.SetAsync(o.Key, o.Value);
        });
        await Task.WhenAll(tasks);
    }
    
    private async Task<Dictionary<string, BlockStateChange>> AddBlockStateChangeAsync(string chainId, long blockHeight, List<BlockStateChange> blockStateChanges)
    {
        if(!_blockStateChanges.TryGetValue(chainId, out var changes))
        {
            changes = new ConcurrentDictionary<long, Dictionary<string, BlockStateChange>>();
            _blockStateChanges[chainId] = changes;
        }

        if (!changes.TryGetValue(blockHeight, out var blockChange))
        {
            var stateChange = await GetBlockStateChangeFromGrainAsync(chainId, blockHeight);
            blockChange = stateChange ?? new Dictionary<string, BlockStateChange>();
            _blockStateChanges[chainId][blockHeight] = blockChange;
        }

        foreach (var blockStateChange in blockStateChanges)
        {
            blockChange.TryAdd(blockStateChange.Key, blockStateChange);
        }

        return blockChange;
    }

    public async Task<List<BlockStateChange>> GetBlockStateChangeAsync(string chainId, long blockHeight)
    {
        if(!_blockStateChanges.TryGetValue(chainId, out var changes))
        {
            changes = new ConcurrentDictionary<long, Dictionary<string, BlockStateChange>>();
            _blockStateChanges[chainId] = changes;
        }
        
        if(!changes.TryGetValue(blockHeight, out var blockChange))
        {
            blockChange = await GetBlockStateChangeFromGrainAsync(chainId, blockHeight);
            if (blockChange == null)
            {
                return null;
            }

            _blockStateChanges[chainId][blockHeight] = blockChange;
        }

        return blockChange.Values.ToList();
    }

    public async Task CleanBlockStateChangeAsync(string chainId, long libHeight)
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

    private async Task<Dictionary<string, BlockStateChange>> GetBlockStateChangeFromGrainAsync(string chainId, long blockHeight)
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