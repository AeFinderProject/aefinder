using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.BlockStates;
using AeFinder.Grains.Grain.Subscriptions;
using Microsoft.Extensions.Logging;

namespace AeFinder.BackgroundWorker.EventHandler;

public class HandlerHelper
{
    public static async Task ClearStoppedVersionAppDataAsync(IClusterClient clusterClient, string appId, string version,
        List<string> chainIds, ILogger logger)
    {
        //remove AppCodeGrain
        var codeId = GrainIdHelper.GenerateGetAppCodeGrainId(appId, version);
        var appCodeGrain = clusterClient.GetGrain<IAppCodeGrain>(codeId);
        await appCodeGrain.RemoveAsync();
        logger.LogInformation("AppCodeGrain state cleared, appId: {0}, historyVersion: {1}", appId, version);
        
        //remove AppBlockStateSetStatusGrain、BlockPusherInfo、BlockPusher Grain data
        foreach (var chainId in chainIds)
        {
            var appBlockStateSetStatusGrain = clusterClient.GetGrain<IAppBlockStateSetStatusGrain>(
                GrainIdHelper.GenerateAppBlockStateSetStatusGrainId(appId, version, chainId));
            await appBlockStateSetStatusGrain.ClearGrainStateAsync();
            logger.LogInformation(
                "AppBlockStateSetStatusGrain state cleared, appId: {appId}, historyVersion: {version}, chainId:{chainId}",
                appId, version, chainId);

            var blockPusherGrainId = GrainIdHelper.GenerateBlockPusherGrainId(appId, version, chainId);
            var blockPusherGrain = clusterClient.GetGrain<IBlockPusherGrain>(blockPusherGrainId);
            await blockPusherGrain.ClearGrainStateAsync();
            logger.LogInformation(
                "BlockPusherGrain state cleared, appId: {appId}, historyVersion: {version}, chainId:{chainId}",
                appId, version, chainId);

            var blockPusherInfoGrain = clusterClient.GetGrain<IBlockPusherInfoGrain>(blockPusherGrainId);
            await blockPusherInfoGrain.ClearGrainStateAsync();
            logger.LogInformation(
                "BlockPusherInfoGrain state cleared, appId: {appId}, historyVersion: {version}, chainId:{chainId}",
                appId, version, chainId);
        }

        //Record version info for remove AppStateGrain、AppBlockStateChangeGrain grain data
        var appDataClearManagerGrain =
            clusterClient.GetGrain<IAppDataClearManagerGrain>(GrainIdHelper.GenerateAppDataClearManagerGrainId());
        await appDataClearManagerGrain.AddVersionClearTaskAsync(appId, version);
        logger.LogInformation("Added version clear task, appId: {0}, historyVersion: {1}", appId, version);

        //Clear elastic search indexes of current version
        var appIndexManagerGrain = clusterClient
            .GetGrain<IAppIndexManagerGrain>(
                GrainIdHelper.GenerateAppIndexManagerGrainId(appId, version));
        await appIndexManagerGrain.ClearVersionIndexAsync();
        await appIndexManagerGrain.ClearGrainStateAsync();
        logger.LogInformation("Elasticsearch index cleared, appId: {0}, historyVersion: {1}", appId, version);
    }
}