using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AeFinder.BackgroundWorker.EventHandler;

public class AppUpgradeHandler : IDistributedEventHandler<AppUpgradeEto>, ITransientDependency
{
    private readonly ILogger<AppUpgradeHandler> _logger;
    private readonly IAppDeployManager _kubernetesAppManager;
    private readonly IClusterClient _clusterClient;
    private readonly IAppService _appService;

    public AppUpgradeHandler(ILogger<AppUpgradeHandler> logger, IAppDeployManager kubernetesAppManager,
        IAppService appService, IClusterClient clusterClient)
    {
        _logger = logger;
        _kubernetesAppManager = kubernetesAppManager;
        _clusterClient = clusterClient;
        _appService = appService;
    }

    public async Task HandleEventAsync(AppUpgradeEto eventData)
    {
        await _kubernetesAppManager.DestroyAppAsync(eventData.AppId, eventData.CurrentVersion);
        _logger.LogInformation("destroy app pod, appId: {0}, pendingVersion: {1} currentVersion: {2}", eventData.AppId, eventData.PendingVersion, eventData.CurrentVersion);

        var appId = eventData.AppId;
        var historyVersion = eventData.CurrentVersion;

        //remove AppCodeGrain
        var codeId = GrainIdHelper.GenerateGetAppCodeGrainId(appId, historyVersion);
        var appCodeGrain= _clusterClient.GetGrain<IAppCodeGrain>(codeId);
        await appCodeGrain.RemoveAsync();
        _logger.LogInformation("AppCodeGrain state cleared, appId: {0}, historyVersion: {1}", appId, historyVersion);
        
        //remove AppBlockStateSetStatusGrain、BlockPusherInfo、BlockPusher Grain data
        foreach (var chainId in eventData.CurrentVersionChainIds)
        {
            var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, historyVersion, chainId));
            await appBlockStateSetStatusGrain.ClearGrainStateAsync();
            _logger.LogInformation("AppBlockStateSetStatusGrain state cleared, appId: {0}, historyVersion: {1}", appId, historyVersion);
            
            var blockPusherGrainId = GrainIdHelper.GenerateBlockPusherGrainId(appId, historyVersion, chainId);
            var blockPusherGrain = _clusterClient.GetGrain<IBlockPusherGrain>(blockPusherGrainId);
            await blockPusherGrain.ClearGrainStateAsync();
            _logger.LogInformation("BlockPusherGrain state cleared, appId: {0}, historyVersion: {1}", appId, historyVersion);
            
            var blockPusherInfoGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(blockPusherGrainId);
            await blockPusherInfoGrain.ClearGrainStateAsync();
            _logger.LogInformation("BlockPusherInfoGrain state cleared, appId: {0}, historyVersion: {1}", appId, historyVersion);
        }
        
        //remove AppStateGrain、AppBlockStateChangeGrain grain data
        await _appService.ClearAppHistoryVersionGrainStateAsync(appId, historyVersion);
        
        //Clear elastic search indexes of current version
        var appIndexManagerGrain=_clusterClient
            .GetGrain<IAppIndexManagerGrain>(
                GrainIdHelper.GenerateAppIndexManagerGrainId(appId, historyVersion));
        await appIndexManagerGrain.ClearVersionIndexAsync();
        await appIndexManagerGrain.ClearGrainStateAsync();
    }
}