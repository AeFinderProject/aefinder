using AeFinder.Block.Dtos;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Sdk;
using AeFinder.Sdk.Entities;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AeFinder.App.BlockState;

public class AppBlockStateInitializationProvider : IAppBlockStateInitializationProvider,ITransientDependency
{
    private readonly IAppDataIndexManagerProvider _appDataIndexManagerProvider;
    private readonly IAppBlockStateChangeProvider _appBlockStateChangeProvider;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IAppStateProvider _appStateProvider;
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IGeneralAppDataIndexProvider _generalAppDataIndexProvider;
    private readonly IRuntimeTypeProvider _runtimeTypeProvider;
    private readonly ILogger<AppBlockStateInitializationProvider> _logger;

    public AppBlockStateInitializationProvider(IAppDataIndexManagerProvider appDataIndexManagerProvider,
        IAppBlockStateChangeProvider appBlockStateChangeProvider, IAppBlockStateSetProvider appBlockStateSetProvider,
        IAppStateProvider appStateProvider, IAppInfoProvider appInfoProvider, IClusterClient clusterClient,
        IGeneralAppDataIndexProvider generalAppDataIndexProvider, IRuntimeTypeProvider runtimeTypeProvider,
        ILogger<AppBlockStateInitializationProvider> logger)
    {
        _appDataIndexManagerProvider = appDataIndexManagerProvider;
        _appBlockStateChangeProvider = appBlockStateChangeProvider;
        _appBlockStateSetProvider = appBlockStateSetProvider;
        _appStateProvider = appStateProvider;
        _appInfoProvider = appInfoProvider;
        _clusterClient = clusterClient;
        _generalAppDataIndexProvider = generalAppDataIndexProvider;
        _runtimeTypeProvider = runtimeTypeProvider;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await Task.Delay(10000);
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(
                GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId));
        var subscription = await appSubscriptionGrain.GetSubscriptionAsync(_appInfoProvider.Version);
        foreach (var item in subscription.SubscriptionItems)
        {
            await RollbackToLibAsync(item.ChainId);
        }
    }

    private async Task RollbackToLibAsync(string chainId)
    {
        var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
            GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _appInfoProvider.Version,
                chainId));
        var status = await appBlockStateSetStatusGrain.GetBlockStateSetStatusAsync();

        if (status.LastIrreversibleBlockHash != null)
        {
            var libBlockStateSet = new BlockStateSet
            {
                Block = new BlockWithTransactionDto
                {
                    BlockHash = status.LastIrreversibleBlockHash,
                    BlockHeight = status.LastIrreversibleBlockHeight,
                    PreviousBlockHash = string.Empty,
                    Confirmed = true
                },
                Processed = true
            };
            await _appBlockStateSetProvider.AddBlockStateSetAsync(chainId, libBlockStateSet);
            await _appBlockStateSetProvider.SetLastIrreversibleBlockStateSetAsync(chainId,
                libBlockStateSet.Block.BlockHash);
        }

        if (status.BestChainBlockHash != null)
        {
            var libBlockIndex = status.LastIrreversibleBlockHash == null
                ? null
                : new BlockIndex(status.LastIrreversibleBlockHash, status.LastIrreversibleBlockHeight);

            var height = status.BestChainHeight;
            var blockStateChanges = await _appBlockStateChangeProvider.GetBlockStateChangeAsync(chainId, height);
            while (blockStateChanges != null)
            {
                foreach (var blockStateChange in blockStateChanges)
                {
                    await RollbackStateAsync(chainId, blockStateChange.Key, blockStateChange.Type, libBlockIndex);
                }

                height--;
                blockStateChanges = await _appBlockStateChangeProvider.GetBlockStateChangeAsync(chainId, height);
            }
            await _appDataIndexManagerProvider.SavaDataAsync();
        }


        if (status.LastIrreversibleBlockHash != null)
        {
            await _appBlockStateSetProvider.SetBestChainBlockStateSetAsync(chainId,
                status.LastIrreversibleBlockHash);
            await _appBlockStateSetProvider.SetLongestChainBlockStateSetAsync(chainId,
                status.LastIrreversibleBlockHash);
            _logger.LogInformation(
                "Rollback block state to lib. ChainId: {ChainId} LibHash: {blockHash}, LibHeight: {libHeight}",
                chainId, status.LastIrreversibleBlockHash, status.LastIrreversibleBlockHeight);
        }

        await _appBlockStateSetProvider.SaveDataAsync(chainId);
    }

    private async Task RollbackStateAsync(string chainId, string key, string typeName, BlockIndex blockIndex)
    {
        var type = _runtimeTypeProvider.GetType(typeName);
        if (blockIndex == null)
        {
            var entity = CreateEntity(type,key);
            await _generalAppDataIndexProvider.DeleteAsync(entity, type);
        }
        else
        {
            var libState = await _appStateProvider.GetStateAsync(chainId, key, blockIndex);
            if (libState == null)
            {
                var entity = CreateEntity(type,key);
                await _generalAppDataIndexProvider.DeleteAsync(entity, type);
            }
            else
            {
                await _generalAppDataIndexProvider.AddOrUpdateAsync(libState, type);
            }
        }
    }

    private object CreateEntity(Type type, string id)
    {
        var entity = Activator.CreateInstance(type);
        var propertyInfo = type.GetProperty(nameof(AeFinderEntity.Id));
        propertyInfo.SetValue(entity, id);

        return entity;
    }
}