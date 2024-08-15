using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AeFinder.BackgroundWorker.EventHandler;

public abstract class AppHandlerBase
{
    public IAbpLazyServiceProvider LazyServiceProvider { get; set; }

    protected IClusterClient ClusterClient => LazyServiceProvider.LazyGetRequiredService<IClusterClient>();

    protected ILogger<AppHandlerBase> Logger => LazyServiceProvider.LazyGetService<ILogger<AppHandlerBase>>();

    protected IObjectMapper ObjectMapper => LazyServiceProvider.LazyGetRequiredService<IObjectMapper>();

    protected async Task ClearStoppedVersionAppDataAsync(string appId, string version,
        List<string> chainIds)
    {
        //remove AppCodeGrain
        var codeId = GrainIdHelper.GenerateGetAppCodeGrainId(appId, version);
        var appCodeGrain = ClusterClient.GetGrain<IAppCodeGrain>(codeId);
        await appCodeGrain.RemoveAsync();
        Logger.LogInformation("AppCodeGrain state cleared, appId: {0}, historyVersion: {1}", appId, version);

        //remove AppBlockStateSetStatusGrain、BlockPusherInfo、BlockPusher Grain data
        foreach (var chainId in chainIds)
        {
            var appBlockStateSetStatusGrain = ClusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, version, chainId));
            await appBlockStateSetStatusGrain.ClearGrainStateAsync();
            Logger.LogInformation(
                "AppBlockStateSetStatusGrain state cleared, appId: {appId}, historyVersion: {version}, chainId:{chainId}",
                appId, version, chainId);

            var blockPusherGrainId = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, chainId);
            var blockPusherGrain = ClusterClient.GetGrain<IBlockPusherGrain>(blockPusherGrainId);
            await blockPusherGrain.ClearGrainStateAsync();
            Logger.LogInformation(
                "BlockPusherGrain state cleared, appId: {appId}, historyVersion: {version}, chainId:{chainId}",
                appId, version, chainId);

            var blockPusherInfoGrain = ClusterClient.GetGrain<IBlockPusherInfoGrain>(blockPusherGrainId);
            await blockPusherInfoGrain.ClearGrainStateAsync();
            Logger.LogInformation(
                "BlockPusherInfoGrain state cleared, appId: {appId}, historyVersion: {version}, chainId:{chainId}",
                appId, version, chainId);
        }

        //Record version info for remove AppStateGrain、AppBlockStateChangeGrain grain data
        var appDataClearManagerGrain =
            ClusterClient.GetGrain<IAppDataClearManagerGrain>(GrainIdHelper.GenerateAppDataClearManagerGrainId());
        await appDataClearManagerGrain.AddVersionClearTaskAsync(appId, version);
        Logger.LogInformation("Added version clear task, appId: {0}, historyVersion: {1}", appId, version);

        //Clear elastic search indexes of current version
        var appIndexManagerGrain = ClusterClient
            .GetGrain<IAppIndexManagerGrain>(
                GrainIdHelper.GenerateAppIndexManagerGrainId(appId, version));
        await appIndexManagerGrain.ClearVersionIndexAsync();
        await appIndexManagerGrain.ClearGrainStateAsync();
        Logger.LogInformation("Elasticsearch index cleared, appId: {0}, historyVersion: {1}", appId, version);
    }
}