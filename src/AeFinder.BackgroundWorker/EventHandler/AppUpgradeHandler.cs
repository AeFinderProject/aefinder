using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Grains;
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

    public AppUpgradeHandler(ILogger<AppUpgradeHandler> logger, IAppDeployManager kubernetesAppManager,IClusterClient clusterClient)
    {
        _logger = logger;
        _kubernetesAppManager = kubernetesAppManager;
        _clusterClient = clusterClient;
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
        
        //remove AppBlockStateSetStatusGrain、BlockPusherInfo、BlockPusher Grain data
        foreach (var chainId in eventData.CurrentVersionChainIds)
        {
            var appBlockStateSetStatusGrain = _clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, historyVersion, chainId));
            await appBlockStateSetStatusGrain.ClearGrainStateAsync();
            
            var blockPusherGrainId = GrainIdHelper.GenerateBlockPusherGrainId(appId, historyVersion, chainId);
            var blockPusherGrain = _clusterClient.GetGrain<IBlockPusherGrain>(blockPusherGrainId);
            await blockPusherGrain.ClearGrainStateAsync();
            
            var blockPusherInfoGrain = _clusterClient.GetGrain<IBlockPusherInfoGrain>(blockPusherGrainId);
            await blockPusherInfoGrain.ClearGrainStateAsync();
        }
        
        
        //Todo remove AppStateGrain、AppBlockStateChangeGrain grain data

    }
}