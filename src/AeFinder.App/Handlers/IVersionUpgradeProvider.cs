using AeFinder.App.BlockState;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Studio.Eto;
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
    private readonly IDistributedEventBus _distributedEventBus;
    private const long UpgradeHeightThreshold = 1000;

    private string _currentVersion = null;
    private readonly Dictionary<string, long> _currentVersionConfirmedBlockHeights = new();
    private List<string> _newVersionChains = new();

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
        await _distributedEventBus.PublishAsync(new AppUpgradeEto()
        {
            AppId = _appInfoProvider.AppId,
            CurrentVersion = _currentVersion,
            NewVersion = _appInfoProvider.Version
        });
        _currentVersion = _appInfoProvider.Version;
    }

    private async Task InitAsync()
    {
        var allSubscription = await _clusterClient
            .GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(_appInfoProvider.AppId))
            .GetAllSubscriptionAsync();

        _currentVersion = allSubscription.CurrentVersion.Version;

        _newVersionChains = allSubscription.NewVersion?.SubscriptionManifest.SubscriptionItems.Select(o => o.ChainId)
            .ToList();
    }

    private async Task<bool> IsThresholdExceededAsync()
    {
        foreach (var chainId in _newVersionChains)
        {
            var newVersionConfirmedBlockHeight =
                (await _appBlockStateSetProvider.GetLastIrreversibleBlockStateSetAsync(chainId))?.Block.BlockHeight;
            if (newVersionConfirmedBlockHeight == null ||
                newVersionConfirmedBlockHeight.Value + UpgradeHeightThreshold <
                _currentVersionConfirmedBlockHeights.GetValueOrDefault(chainId, 0))
            {
                return false;
            }
        }

        return true;
    }

    private async Task UpdateCurrentVersionConfirmedBlockHeightsAsync()
    {
        foreach (var chainId in _newVersionChains)
        {
            var grain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(_appInfoProvider.AppId, _currentVersion, chainId));
            var status = await grain.GetBlockStateSetStatusAsync();
            _currentVersionConfirmedBlockHeights[chainId] = status.LastIrreversibleBlockHeight;
        }
    }
}