using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AeFinder.BlockScan;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class BlockScanAppService : AeFinderAppService, IBlockScanAppService
{
    private readonly IClusterClient _clusterClient;

    public BlockScanAppService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<List<Guid>> GetMessageStreamIdsAsync(string appId, string version, string chainId = null)
    {
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = await client.GetSubscriptionAsync(version);
        var streamIds = new List<Guid>();
        foreach (var subscriptionItem in subscription.SubscriptionItems)
        {
            if (!chainId.IsNullOrWhiteSpace() && subscriptionItem.ChainId != chainId)
            {
                continue;
            }
            
            var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, subscriptionItem.ChainId);
            var blockScanInfoGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(id);
            var streamId = await blockScanInfoGrain.GetMessageStreamIdAsync();
            streamIds.Add(streamId);
        }

        return streamIds;
    }

    public async Task StartScanAsync(string appId, string version, string chainId = null)
    {
        Logger.LogInformation("ScanApp: {appId} start scan, version: {version}", appId, version);

        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var subscription = await client.GetSubscriptionAsync(version);
        var scanToken = Guid.NewGuid().ToString("N");
        await client.StartAsync(version);
        foreach (var subscriptionItem in subscription.SubscriptionItems)
        {
            if (!chainId.IsNullOrWhiteSpace() && subscriptionItem.ChainId != chainId)
            {
                continue;
            }

            var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, subscriptionItem.ChainId);
            var blockScanGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(id);
            var blockScanExecutorGrain = _clusterClient.GetGrain<IBlockPusherGrain>(id);

            var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, version, subscriptionItem.ChainId));
            var startBlockHeight = (await appBlockStateSetStatusGrain.GetBlockStateSetStatusAsync())
                .LastIrreversibleBlockHeight;
            if (startBlockHeight == 0)
            {
                startBlockHeight = subscriptionItem.StartBlockNumber;
            }
            else
            {
                startBlockHeight += 1;
            }

            await blockScanGrain.InitializeAsync(appId, version, subscriptionItem, scanToken);
            await blockScanExecutorGrain.InitializeAsync(scanToken, startBlockHeight);

            Logger.LogDebug("Start ScanApp: {appId}, id: {id}", appId, id);
            _ = Task.Run(blockScanExecutorGrain.HandleHistoricalBlockAsync);
        }
    }

    public async Task UpgradeVersionAsync(string appId, string version)
    {
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        await client.UpgradeVersionAsync(version);
    }

    public async Task<AllSubscriptionDto> GetSubscriptionAsync(string appId)
    {
        var clientGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var allSubscription = await clientGrain.GetAllSubscriptionAsync();
        return ObjectMapper.Map<AllSubscription, AllSubscriptionDto>(allSubscription);
    }

    public async Task PauseAsync(string appId, string version)
    {
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        var scanManager = _clusterClient.GetGrain<IBlockPusherManagerGrain>(0);
        var subscription = await client.GetSubscriptionAsync(version);
        await client.PauseAsync(version);
        foreach (var subscriptionItem in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, subscriptionItem.ChainId);
            await scanManager.RemoveBlockPusherAsync(subscriptionItem.ChainId, id);
        }
    }

    public async Task StopAsync(string appId, string version)
    {
        var clientGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        
        Logger.LogInformation("ScanApp: {clientId} start stop scan, version: {version}", appId, version);
        await clientGrain.StopAsync(version);
        Logger.LogInformation("ScanApp: {clientId} stopped , version: {version}", appId, version);
    }

    public async Task<bool> IsRunningAsync(string chainId, string appId, string version, string token)
    {
        var clientGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId));
        return await clientGrain.IsRunningAsync(version, chainId, token);
    }
}