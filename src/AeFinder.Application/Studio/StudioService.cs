using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.BlockScan;
using AeFinder.CodeOps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace AeFinder.Studio;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class StudioService : AeFinderAppService, IStudioService, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    public StudioService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<AppBlockStateMonitorDto> MonitorAppBlockStateAsync(string appId)
    {
        if (appId.IsNullOrEmpty())
        {
            throw new UserFriendlyException("invalid appId.");
        }
        
        var result = new AppBlockStateMonitorDto();
        var appSubscriptionGrain =
            _clusterClient.GetGrain<IAppSubscriptionGrain>(
                GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscription = await appSubscriptionGrain.GetAllSubscriptionAsync();
        if (allSubscription == null)
        {
            return result;
        }

        if (allSubscription.CurrentVersion != null)
        {
            var currentVersionBlockStates = new List<MonitorBlockState>();
            var version = allSubscription.CurrentVersion.Version;
            var subscription = allSubscription.CurrentVersion.SubscriptionManifest;
            foreach (var subscriptionItem in subscription.SubscriptionItems)
            {
                var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                    GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, version, subscriptionItem.ChainId));
                var blockStateSetStatus = await appBlockStateSetStatusGrain.GetBlockStateSetStatusAsync();
                var monitorBlockState = new MonitorBlockState()
                {
                    ChainId = subscriptionItem.ChainId,
                    AppId = appId,
                    Version = version,
                    LongestChainBlockHash = blockStateSetStatus.LongestChainBlockHash,
                    LongestChainHeight = blockStateSetStatus.LongestChainHeight,
                    BestChainBlockHash = blockStateSetStatus.BestChainBlockHash,
                    BestChainHeight = blockStateSetStatus.BestChainHeight,
                    LastIrreversibleBlockHash = blockStateSetStatus.LastIrreversibleBlockHash,
                    LastIrreversibleBlockHeight = blockStateSetStatus.LastIrreversibleBlockHeight
                };
                currentVersionBlockStates.Add(monitorBlockState);
            }

            result.CurrentVersionBlockStates = currentVersionBlockStates;
        }

        if (allSubscription.NewVersion != null)
        {
            var newVersionBlockStates = new List<MonitorBlockState>();
            var version = allSubscription.NewVersion.Version;
            var subscription = allSubscription.NewVersion.SubscriptionManifest;
            foreach (var subscriptionItem in subscription.SubscriptionItems)
            {
                var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                    GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, version, subscriptionItem.ChainId));
                var blockStateSetStatus = await appBlockStateSetStatusGrain.GetBlockStateSetStatusAsync();
                var monitorBlockState = new MonitorBlockState()
                {
                    ChainId = subscriptionItem.ChainId,
                    AppId = appId,
                    Version = version,
                    LongestChainBlockHash = blockStateSetStatus.LongestChainBlockHash,
                    LongestChainHeight = blockStateSetStatus.LongestChainHeight,
                    BestChainBlockHash = blockStateSetStatus.BestChainBlockHash,
                    BestChainHeight = blockStateSetStatus.BestChainHeight,
                    LastIrreversibleBlockHash = blockStateSetStatus.LastIrreversibleBlockHash,
                    LastIrreversibleBlockHeight = blockStateSetStatus.LastIrreversibleBlockHeight
                };
                newVersionBlockStates.Add(monitorBlockState);
            }

            result.NewVersionBlockStates = newVersionBlockStates;
        }

        return result;
    }
}