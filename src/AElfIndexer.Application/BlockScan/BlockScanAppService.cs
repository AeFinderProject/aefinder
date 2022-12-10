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

    public async Task<string> SubmitSubscriptionInfoAsync(string clientId, List<SubscriptionInfo> subscriptionInfos)
    {
        Logger.LogInformation($"Client: {clientId} submit subscribe: {JsonSerializer.Serialize(subscriptionInfos)}");

        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        var oldVersion = (await client.GetVersionAsync()).NewVersion;
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

        var version = await client.AddSubscriptionInfoAsync(subscriptionInfos);
        return version;
    }

    public async Task<List<Guid>> GetMessageStreamIdsAsync(string clientId, string version)
    {
        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        var subscriptionInfos = await client.GetSubscriptionInfoAsync(version);
        var streamIds = new List<Guid>();
        foreach (var subscriptionInfo in subscriptionInfos)
        {
            var id = subscriptionInfo.ChainId + clientId + version + subscriptionInfo.FilterType;
            var blockScanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(id);
            var streamId = await blockScanInfoGrain.GetMessageStreamIdAsync();
            streamIds.Add(streamId);
        }

        return streamIds;
    }

    public async Task StartScanAsync(string clientId, string version)
    {
        Logger.LogInformation($"Client: {clientId} start scan, version: {version}");

        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        var subscriptionInfos = await client.GetSubscriptionInfoAsync(version);
        var versionStatus = await client.GetVersionStatusAsync(version);
        foreach (var subscriptionInfo in subscriptionInfos)
        {
            var id = subscriptionInfo.ChainId + clientId + version + subscriptionInfo.FilterType;
            var blockScanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(id);
            var scanGrain = _clusterClient.GetGrain<IBlockScanGrain>(id);

            if (versionStatus == VersionStatus.Initialized)
            {
                await client.AddBlockScanIdAsync(version, id);
                await blockScanInfoGrain.InitializeAsync(subscriptionInfo.ChainId, clientId, version, subscriptionInfo);
                await scanGrain.InitializeAsync(subscriptionInfo.ChainId, clientId, version);
            }

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
        var currentVersion = (await client.GetVersionAsync()).CurrentVersion;
        var scanIds = await client.GetBlockScanIdsAsync(currentVersion);
        foreach (var scanId in scanIds)
        {
            var blockScanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(scanId);
            await blockScanInfoGrain.StopAsync();
        }

        await client.UpgradeVersionAsync();
    }
    
    public async Task<ClientVersionDto> GetClientVersionAsync(string clientId)
    {
        var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
        var version = await clientGrain.GetVersionAsync();
        return new ClientVersionDto
        {
            CurrentVersion = version.CurrentVersion,
            NewVersion = version.NewVersion
        };
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