using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.State.BlockScan;
using Microsoft.Extensions.Logging;
using Nest;
using NUglify.Helpers;
using Orleans;
using Orleans.Streams;
using Volo.Abp;

namespace AElfIndexer.BlockScan;

[RemoteService(IsEnabled = false)]
public class BlockScanAppService : AElfIndexerAppService, IBlockScanAppService
{
    private readonly IClusterClient _clusterClient;

    public BlockScanAppService(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<string> SubmitSubscribeInfoAsync(string clientId, List<SubscribeInfo> subscribeInfos)
    {
        Logger.LogInformation($"Client: {clientId} submit subscribe: {JsonSerializer.Serialize(subscribeInfos)}");

        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        var oldVersion = await client.GetNewVersionAsync();
        if (!string.IsNullOrWhiteSpace(oldVersion))
        {
            var scanIds = await client.GetBlockScanIdsAsync(oldVersion);
            foreach (var id in scanIds)
            {
                var scanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(id);
                await scanInfoGrain.StopAsync();
            }

            await client.RemoveVersionInfoAsync(oldVersion);
        }

        var version = await client.AddSubscribeInfoAsync(subscribeInfos);
        return version;
    }

    public async Task<List<string>> GetMessageStreamIdsAsync(string clientId, string version)
    {
        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        var subscribeInfos = await client.GetSubscribeInfoAsync(version);

        return subscribeInfos
            .Select(subscribeInfo => subscribeInfo.ChainId + clientId + version + subscribeInfo.FilterType).ToList();
    }

    public async Task StartScanAsync(string clientId, string version)
    {
        Logger.LogInformation($"Client: {clientId} start scan, version: {version}");

        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        var subscribeInfos = await client.GetSubscribeInfoAsync(version);
        var versionStatus = await client.GetVersionStatus(version);
        foreach (var subscribeInfo in subscribeInfos)
        {
            var id = subscribeInfo.ChainId + clientId + version + subscribeInfo.FilterType;
            var blockScanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(id);
            var scanGrain = _clusterClient.GetGrain<IBlockScanGrain>(id);

            if (versionStatus == VersionStatus.Initialized)
            {
                await client.AddBlockScanIdAsync(version, id);
                await blockScanInfoGrain.InitializeAsync(subscribeInfo.ChainId, clientId, version, subscribeInfo);
                await scanGrain.InitializeAsync(subscribeInfo.ChainId, clientId, version);
            }

            // var streamId = await blockScanInfoGrain.GetMessageStreamIdAsync();
            // var stream =
            //     _clusterClient
            //         .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
            //         .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);
            //
            // var subscriptionHandles = await stream.GetAllSubscriptionHandles();
            // if (!subscriptionHandles.IsNullOrEmpty())
            // {
            //     subscriptionHandles.ForEach(async x => await x.ResumeAsync<SubscribedBlockDto>(onNextAsync));
            // }
            // else
            // {
            //     await stream.SubscribeAsync<SubscribedBlockDto>(onNextAsync);
            // }
            
            _ = Task.Run(scanGrain.HandleHistoricalBlockAsync);
        }
        
        await client.StartAsync(version);
    }
    
    public static async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken? token = null)
    {
        Console.WriteLine($"========= Version: {subscribedBlock.Version}");
    }

    public async Task UpgradeVersionAsync(string clientId)
    {
        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        var currentVersion = await client.GetCurrentVersionAsync();
        var scanIds = await client.GetBlockScanIdsAsync(currentVersion);
        foreach (var scanId in scanIds)
        {
            var blockScanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(scanId);
            await blockScanInfoGrain.StopAsync();
        }

        await client.UpgradeVersionAsync();
    }

    public async Task<bool> IsVersionAvailableAsync(string clientId, string version)
    {
        var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
        return await clientGrain.IsVersionAvailableAsync(version);
    }

    public async Task StopAsync(string clientId, string version)
    {
        //TODO: Maybe no need?
    }
}