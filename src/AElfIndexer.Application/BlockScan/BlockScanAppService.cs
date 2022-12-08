using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.BlockScan;
using Microsoft.Extensions.Logging;
using Nest;
using Orleans;
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

    public async Task SubscribeAsync(string clientId, List<SubscribeInfo> subscribeInfos)
    {
        Logger.LogInformation($"Client: {clientId} request subscribe: {JsonSerializer.Serialize(subscribeInfos)}");

        var client = _clusterClient.GetGrain<ClientGrain>(clientId);
        var version = await client.SubscribeAsync(subscribeInfos);

        foreach (var subscribeInfo in subscribeInfos)
        {
            var id = subscribeInfo.ChainId + clientId + version + subscribeInfo.FilterType;
            
            var clientGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(id);
            await clientGrain.InitializeAsync(subscribeInfo.ChainId, clientId, version, subscribeInfo);
            
            var scanGrain = _clusterClient.GetGrain<IBlockScanGrain>(id);
            await scanGrain.InitializeAsync(subscribeInfo.ChainId, clientId, version);
            
            _ = Task.Run(scanGrain.HandleHistoricalBlockAsync);
        }
    }
}