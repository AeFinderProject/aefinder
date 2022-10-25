using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using AElfScan.Orleans;
using AElfScan.Orleans.EventSourcing.Grain.Chains;
using AElfScan.Orleans.EventSourcing.Grain.ScanClients;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using NUglify.Helpers;
using Orleans;
using Orleans.Streams;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.Clients;

namespace AElfScan.Hubs;

public class BlockHub : AbpHub
{
    private readonly ISubscribedBlockHandler _subscribedBlockHandler;
    private readonly IConnectionProvider _connectionProvider;
    private readonly IClusterClientAppService _clusterClientAppService;

    public BlockHub(ISubscribedBlockHandler subscribedBlockHandler,
        IConnectionProvider connectionProvider, IClusterClientAppService clusterClientAppService)
    {
        _subscribedBlockHandler = subscribedBlockHandler;
        _connectionProvider = connectionProvider;
        _clusterClientAppService = clusterClientAppService;
    }

    [Authorize]
    public async Task Subscribe(List<SubscribeInfo> subscribeInfos)
    {
        var client = _clusterClientAppService.Client;
        var clientId = Context.User.FindFirst(o=>o.ToString().StartsWith("client_id")).Value;
        _connectionProvider.Add(clientId,Context.ConnectionId);

        foreach (var subscribeInfo in subscribeInfos)
        {
            var id = subscribeInfo.ChainId + clientId;
            var clientGrain = client.GetGrain<IClientGrain>(id);
            var version = await clientGrain.InitializeAsync(subscribeInfo.ChainId, clientId, subscribeInfo);

            var scanGrain = client.GetGrain<IBlockScanGrain>(id);
            var streamId = await scanGrain.InitializeAsync(subscribeInfo.ChainId, clientId, version);
            var stream =
                client
                    .GetStreamProvider("AElfScan")
                    .GetStream<SubscribedBlockDto>(streamId, "default");
            
            var subscriptionHandles = await stream.GetAllSubscriptionHandles();
            if (!subscriptionHandles.IsNullOrEmpty())
            {
                subscriptionHandles.ForEach(
                    async x => await x.ResumeAsync(_subscribedBlockHandler.HandleAsync));
            }
            else
            {
                await stream.SubscribeAsync(_subscribedBlockHandler.HandleAsync);
            }
            
            Task.Run(scanGrain.HandleHistoricalBlockAsync);
        }
    }
}