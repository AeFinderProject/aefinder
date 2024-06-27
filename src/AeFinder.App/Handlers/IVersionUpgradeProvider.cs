using AeFinder.App.BlockState;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Threading;

namespace AeFinder.App.Handlers;

public interface IVersionUpgradeProvider
{
    Task UpgradeAsync();
}

public class VersionUpgradeProvider : IVersionUpgradeProvider, ISingletonDependency
{
    private readonly IAppInfoProvider _appInfoProvider;
    private readonly IAppBlockStateSetProvider _appBlockStateSetProvider;
    private readonly IClusterClient _clusterClient;
    private const long UpgradeHeightThreshold = 1000;

    private string _currentVersion = null;
    private readonly Dictionary<string, long> _currentVersionConfirmedBlockHeights = new();
    private List<string> _pendingVersionChains = new();

    public VersionUpgradeProvider(IAppInfoProvider appInfoProvider, IClusterClient clusterClient, IAppBlockStateSetProvider appBlockStateSetProvider)
    {
        _appInfoProvider = appInfoProvider;
        _clusterClient = clusterClient;
        _appBlockStateSetProvider = appBlockStateSetProvider;

        AsyncHelper.RunSync(InitAsync);
    }

    public async Task UpgradeAsync()
    {
        if (_currentVersion == null || _currentVersion == _appInfoProvider.Version)
        {
            return;
        }

        if (!await IsThresholdExceededAsync())
        {
            return;
        }

        await UpdateCurrentVersionConfirmedBlockHeightsAsync();

        if (!await IsThresholdExceededAsync())
        {
            return;
        }

        await _clusterClient
            .GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId))
            .UpgradeVersionAsync();
        _currentVersion = _appInfoProvider.Version;
    }

    private async Task InitAsync()
    {
        var allSubscription = await _clusterClient
            .GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId))
            .GetAllSubscriptionAsync();

        _currentVersion = allSubscription.CurrentVersion.Version;

        _pendingVersionChains = allSubscription.PendingVersion?.SubscriptionManifest.SubscriptionItems.Select(o => o.ChainId)
            .ToList();
    }

    private async Task<bool> IsThresholdExceededAsync()
    {
        foreach (var chainId in _pendingVersionChains)
        {
            var pendingVersionConfirmedBlockHeight =
                (await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId))?.Block.BlockHeight;
            if (pendingVersionConfirmedBlockHeight == null ||
                pendingVersionConfirmedBlockHeight.Value + UpgradeHeightThreshold <
                _currentVersionConfirmedBlockHeights.GetValueOrDefault(chainId, 0))
            {
                return false;
            }
        }

        return true;
    }

    private async Task UpdateCurrentVersionConfirmedBlockHeightsAsync()
    {
        foreach (var chainId in _pendingVersionChains)
        {
            var grain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _currentVersion, chainId));
            var status = await grain.GetBlockStateSetStatusAsync();
            _currentVersionConfirmedBlockHeights[chainId] = status.LastIrreversibleBlockHeight;
        }
    }
}