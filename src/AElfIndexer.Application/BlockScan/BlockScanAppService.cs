using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.BlockScan;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace AElfIndexer.BlockScan;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
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
    
    public async Task UpdateSubscriptionInfoAsync(string clientId, string version, List<SubscriptionInfo> subscriptionInfos)
    {
        Logger.LogInformation($"Client: {clientId} update subscribe: {JsonSerializer.Serialize(subscriptionInfos)}");

        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        await client.UpdateSubscriptionInfoAsync(version, subscriptionInfos);
    }

    public async Task<List<Guid>> GetMessageStreamIdsAsync(string clientId, string version)
    {
        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        var subscriptionInfos = await client.GetSubscriptionInfoAsync(version);
        var streamIds = new List<Guid>();
        foreach (var subscriptionInfo in subscriptionInfos)
        {
            var id = GrainIdHelper.GenerateGrainId(subscriptionInfo.ChainId, clientId, version,
                subscriptionInfo.FilterType);
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
        await client.SetTokenAsync(version);
        foreach (var subscriptionInfo in subscriptionInfos)
        {
            var id = GrainIdHelper.GenerateGrainId(subscriptionInfo.ChainId, clientId, version, subscriptionInfo.FilterType);
            var blockScanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(id);
            var scanGrain = _clusterClient.GetGrain<IBlockScanGrain>(id);

            if (versionStatus == VersionStatus.Initialized)
            {
                await client.AddBlockScanIdAsync(version, id);
                await blockScanInfoGrain.InitializeAsync(subscriptionInfo.ChainId, clientId, version, subscriptionInfo);
                await scanGrain.InitializeAsync(subscriptionInfo.ChainId, clientId, version);
            }
            else
            {
                var blockStateSetInfoGrain = _clusterClient.GetGrain<IBlockStateSetInfoGrain>(
                    GrainIdHelper.GenerateGrainId("BlockStateSetInfo", clientId, subscriptionInfo.ChainId, version));
                var blockHeight = await blockStateSetInfoGrain.GetConfirmedBlockHeight(subscriptionInfo.FilterType);
                if (blockHeight == 0) blockHeight = subscriptionInfo.StartBlockNumber-1;
                await scanGrain.ReScanAsync(blockHeight);
            }

            Logger.LogDebug($"Start client: {clientId}, id: {id}");
            _ = Task.Run(scanGrain.HandleHistoricalBlockAsync);
        }
        
        await client.StartAsync(version);
    }

    public async Task UpgradeVersionAsync(string clientId)
    {
        var client = _clusterClient.GetGrain<IClientGrain>(clientId);
        var version = await client.GetVersionAsync();
        if (string.IsNullOrWhiteSpace(version.NewVersion))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(version.CurrentVersion))
        {
            var scanIds = await client.GetBlockScanIdsAsync(version.CurrentVersion);
            foreach (var scanId in scanIds)
            {
                var blockScanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(scanId);
                await blockScanInfoGrain.StopAsync();
            }
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

    public async Task<string> GetClientTokenAsync(string clientId, string version)
    {
        var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
        return await clientGrain.GetTokenAsync(version);
    }

    public async Task<SubscriptionInfoDto> GetSubscriptionInfoAsync(string clientId)
    {
        var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
        var version = await clientGrain.GetVersionAsync();
        var subscriptionInfos = new SubscriptionInfoDto();
        if (!string.IsNullOrWhiteSpace(version.CurrentVersion))
        {
            var subscriptionInfo = await clientGrain.GetSubscriptionInfoAsync(version.CurrentVersion);
            subscriptionInfos.CurrentVersion = new SubscriptionInfoDetailDto
            {
                Version = version.CurrentVersion,
                SubscriptionInfos = subscriptionInfo
            };
        }
        
        if (!string.IsNullOrWhiteSpace(version.NewVersion))
        {
            var subscriptionInfo = await clientGrain.GetSubscriptionInfoAsync(version.NewVersion);
            subscriptionInfos.NewVersion = new SubscriptionInfoDetailDto
            {
                Version = version.NewVersion,
                SubscriptionInfos = subscriptionInfo
            };
        }

        return subscriptionInfos;
    }

    public async Task StopAsync(string clientId, string version)
    {
        var clientGrain = _clusterClient.GetGrain<IClientGrain>(clientId);
        var scanIds = await clientGrain.GetBlockScanIdsAsync(version);
        foreach (var scanId in scanIds)
        {
            var scanInfoGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(scanId);
            await scanInfoGrain.StopAsync();
        }

        await clientGrain.StopAsync(version);
    }
}