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
    private readonly IAppDeployManager _kubernetesAppManager;
    private readonly IAppService _appService;

    public BlockScanAppService(IClusterClient clusterClient, IAppService appService,
        IAppDeployManager kubernetesAppManager)
    {
        _clusterClient = clusterClient;
        _kubernetesAppManager = kubernetesAppManager;
        _appService = appService;
    }

    public async Task<List<Guid>> GetMessageStreamIdsAsync(string clientId, string version)
    {
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        var subscription = await client.GetSubscriptionAsync(version);
        var streamIds = new List<Guid>();
        foreach (var subscriptionItem in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, subscriptionItem.ChainId);
            var blockScanInfoGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(id);
            var streamId = await blockScanInfoGrain.GetMessageStreamIdAsync();
            streamIds.Add(streamId);
        }

        return streamIds;
    }

    public async Task StartScanAsync(string clientId, string version)
    {
        Logger.LogInformation("ScanApp: {clientId} start scan, version: {version}", clientId, version);

        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        var subscription = await client.GetSubscriptionAsync(version);
        var versionStatus = await client.GetSubscriptionStatusAsync(version);
        var scanToken = Guid.NewGuid().ToString("N");
        await client.StartAsync(version);
        foreach (var subscriptionItem in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, subscriptionItem.ChainId);
            var blockScanGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(id);
            var blockScanExecutorGrain = _clusterClient.GetGrain<IBlockPusherGrain>(id);

            var startBlockHeight = subscriptionItem.StartBlockNumber;

            if (versionStatus != SubscriptionStatus.Initialized)
            {
                var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                    GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(clientId, version, subscriptionItem.ChainId));
                // TODO: This is not correct, we should get the confirmed block height from the chain grain.
                startBlockHeight = (await appBlockStateSetStatusGrain.GetBlockStateSetStatusAsync()).LastIrreversibleBlockHeight;
                if (startBlockHeight == 0)
                {
                    startBlockHeight = subscriptionItem.StartBlockNumber;
                }
                else
                {
                    startBlockHeight += 1;
                }
            }

            await blockScanGrain.InitializeAsync(clientId, version, subscriptionItem, scanToken);
            await blockScanExecutorGrain.InitializeAsync(scanToken, startBlockHeight);

            Logger.LogDebug("Start ScanApp: {clientId}, id: {id}", clientId, id);
            _ = Task.Run(blockScanExecutorGrain.HandleHistoricalBlockAsync);
        }
    }

    public async Task UpgradeVersionAsync(string clientId)
    {
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        await client.UpgradeVersionAsync();
    }

    public async Task<AllSubscriptionDto> GetSubscriptionAsync(string clientId)
    {
        var clientGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        var allSubscription = await clientGrain.GetAllSubscriptionAsync();
        return ObjectMapper.Map<AllSubscription, AllSubscriptionDto>(allSubscription);
    }

    public async Task PauseAsync(string clientId, string version)
    {
        var client = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        var scanManager = _clusterClient.GetGrain<IBlockPusherManagerGrain>(0);
        var subscription = await client.GetSubscriptionAsync(version);
        await client.PauseAsync(version);
        foreach (var subscriptionItem in subscription.SubscriptionItems)
        {
            var id = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, subscriptionItem.ChainId);
            await scanManager.RemoveBlockPusherAsync(subscriptionItem.ChainId, id);
        }
    }

    public async Task StopAsync(string clientId, string version)
    {
        var clientGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        var subscriptionManifest = await clientGrain.GetSubscriptionAsync(version);
        
        await _kubernetesAppManager.DestroyAppAsync(clientId, version);
        
        //remove AppBlockStateSetStatusGrain、BlockPusherInfo、BlockPusher Grain data
        foreach (var subscriptionItem in subscriptionManifest.SubscriptionItems)
        {
            var chainId = subscriptionItem.ChainId;
            var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(clientId, version, chainId));
            await appBlockStateSetStatusGrain.ClearGrainStateAsync();
            Logger.LogInformation("AppBlockStateSetStatusGrain state cleared, appId: {appId}, historyVersion: {version}", clientId, version);
            
            var blockPusherGrainId = GrainIdHelper.GenerateBlockPusherGrainId(clientId, version, chainId);
            var blockPusherGrain = _clusterClient.GetGrain<IBlockPusherGrain>(blockPusherGrainId);
            await blockPusherGrain.ClearGrainStateAsync();
            Logger.LogInformation("BlockPusherGrain state cleared, appId: {appId}, historyVersion: {version}", clientId, version);
            
            var blockPusherInfoGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(blockPusherGrainId);
            await blockPusherInfoGrain.ClearGrainStateAsync();
            Logger.LogInformation("BlockPusherInfoGrain state cleared, appId: {appId}, historyVersion: {version}", clientId, version);
        }
        
        //remove AppCodeGrain
        var codeId = GrainIdHelper.GenerateGetAppCodeGrainId(clientId, version);
        var appCodeGrain= _clusterClient.GetGrain<IAppCodeGrain>(codeId);
        await appCodeGrain.RemoveAsync();
        Logger.LogInformation("AppCodeGrain state cleared, appId: {0}, historyVersion: {1}", clientId, version);
        
        //Record version info for remove AppStateGrain、AppBlockStateChangeGrain grain data
        var appDataClearManagerGrain =
            _clusterClient.GetGrain<IAppDataClearManagerGrain>(GrainIdHelper.GenerateAppDataClearManagerGrainId());
        await appDataClearManagerGrain.AddVersionClearTaskAsync(clientId, version);

        //Clear elastic search indexes of current version
        var appIndexManagerGrain=_clusterClient
            .GetGrain<IAppIndexManagerGrain>(
                GrainIdHelper.GenerateAppIndexManagerGrainId(clientId, version));
        await appIndexManagerGrain.ClearVersionIndexAsync();
        await appIndexManagerGrain.ClearGrainStateAsync();
        Logger.LogInformation("ScanApp: {clientId} elastic index cleared , version: {version}", clientId, version);
        
        Logger.LogInformation("ScanApp: {clientId} start stop scan, version: {version}", clientId, version);
        await clientGrain.StopAsync(version);
        Logger.LogInformation("ScanApp: {clientId} stopped , version: {version}", clientId, version);
    }

    public async Task<bool> IsRunningAsync(string chainId, string clientId, string version, string token)
    {
        var clientGrain = _clusterClient.GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(clientId));
        return await clientGrain.IsRunningAsync(version, chainId, token);
    }
}