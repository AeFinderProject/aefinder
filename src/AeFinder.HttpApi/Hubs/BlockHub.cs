using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.BlockScan;
using Microsoft.AspNetCore.Authorization;
using NUglify.Helpers;
using Orleans;
using Orleans.Streams;
using Volo.Abp.AspNetCore.SignalR;

namespace AeFinder.Hubs;

public class BlockHub : AbpHub
{
    private readonly ISubscribedBlockHandler _subscribedBlockHandler;
    private readonly IConnectionProvider _connectionProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IBlockScanAppService _blockScanAppService;

    public BlockHub(ISubscribedBlockHandler subscribedBlockHandler,
        IConnectionProvider connectionProvider, IClusterClient clusterClient, IBlockScanAppService blockScanAppService)
    {
        _subscribedBlockHandler = subscribedBlockHandler;
        _connectionProvider = connectionProvider;
        _clusterClient = clusterClient;
        _blockScanAppService = blockScanAppService;
    }

    [Authorize]
    public async Task Start(string version)
    {
        var clientId = Context.User.FindFirst(o=>o.ToString().StartsWith("client_id")).Value;
        
        _connectionProvider.Add(clientId,Context.ConnectionId,version, new List<string>());

        var messageStreamIds = await _blockScanAppService.GetMessageStreamIdsAsync(clientId, version);
        foreach (var streamId in messageStreamIds)
        {
            var stream =
                _clusterClient
                    .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                    .GetStream<SubscribedBlockDto>(streamId, AeFinderApplicationConsts.MessageStreamNamespace);

            var subscriptionHandles = await stream.GetAllSubscriptionHandles();
            if (!subscriptionHandles.IsNullOrEmpty())
            {
                subscriptionHandles.ForEach(async x =>
                    await x.ResumeAsync(_subscribedBlockHandler.HandleAsync));
            }
            else
            {
                await stream.SubscribeAsync(_subscribedBlockHandler.HandleAsync);
            }
        }
        
        await _blockScanAppService.StartScanAsync(clientId, version);
    }

    // [Authorize]
    // public async Task Subscribe(List<SubscriptionInfo> subscriptionInfos)
    // {
    //     var clientId = Context.User.FindFirst(o=>o.ToString().StartsWith("client_id")).Value;
    //     Logger.LogInformation($"Client: {clientId} request subscribe: {JsonSerializer.Serialize(subscriptionInfos)}");
    //     var version = Guid.NewGuid().ToString("N");
    //     _connectionProvider.Add(clientId,Context.ConnectionId,version, subscriptionInfos.Select(o=>o.ChainId).ToList());
    //     
    //     foreach (var subscriptionInfo in subscriptionInfos)
    //     {
    //         var id = subscriptionInfo.ChainId + clientId + subscriptionInfo.FilterType;
    //         var clientGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(id);
    //         await clientGrain.InitializeAsync(subscriptionInfo.ChainId, clientId, version, subscriptionInfo);
    //         
    //         var scanGrain = _clusterClient.GetGrain<IBlockScanGrain>(id);
    //         var streamId = await scanGrain.InitializeAsync(subscriptionInfo.ChainId, clientId, version);
    //         var stream =
    //             _clusterClient
    //                 .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
    //                 .GetStream<SubscribedBlockDto>(streamId, AeFinderApplicationConsts.MessageStreamNamespace);
    //         
    //         var subscriptionHandles = await stream.GetAllSubscriptionHandles();
    //         if (!subscriptionHandles.IsNullOrEmpty())
    //         {
    //             subscriptionHandles.ForEach(
    //                 async x => await x.ResumeAsync(_subscribedBlockHandler.HandleAsync));
    //         }
    //         else
    //         {
    //             await stream.SubscribeAsync(_subscribedBlockHandler.HandleAsync);
    //         }
    //         
    //         Task.Run(scanGrain.HandleHistoricalBlockAsync);
    //     }
    // }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // var connection = _connectionProvider.GetConnectionByConnectionId(Context.ConnectionId);
        // if (connection != null)
        // {
        //     foreach (var chainId in connection.ChainIds)
        //     {
        //         var id = chainId + connection.ClientId;
        //         var clientGrain = _clusterClient.GetGrain<IBlockScanInfoGrain>(id);
        //         await clientGrain.StopAsync(connection.Version);
        //     }
        //     _connectionProvider.Remove(Context.ConnectionId);
        // }
    }
}