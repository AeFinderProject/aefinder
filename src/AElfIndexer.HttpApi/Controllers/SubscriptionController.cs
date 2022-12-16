using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using AElfIndexer.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NUglify.Helpers;
using Orleans;
using Orleans.Streams;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfIndexer.Controllers;

[RemoteService]
[ControllerName("Subscription")]
[Route("api/app/subscription")]
public class SubscriptionController : AbpController
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IClusterClient _clusterClient;

    public SubscriptionController(IBlockScanAppService blockScanAppService, IClusterClient clusterClient)
    {
        _blockScanAppService = blockScanAppService;
        _clusterClient = clusterClient;
    }
    
    [HttpPut]
    //[Authorize]
    public virtual Task<string> SubmitSubscriptionInfoAsync(string clientId, List<SubscriptionInfo> subscriptionInfos)
    {
        //var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
        return _blockScanAppService.SubmitSubscriptionInfoAsync(clientId,subscriptionInfos);
    }
    
    [HttpGet]
    //[Authorize]
    public virtual Task<SubscriptionInfoDto> GetSubscriptionInfoAsync(string clientId)
    {
        return _blockScanAppService.GetSubscriptionInfoAsync(clientId);
    }
    
    // TODO: Only for Test
    [HttpPost]
    [Route("start")]
    //[Authorize]
    public virtual async Task StartScanAsync(string clientId, string version)
    {
        //var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
        
        var messageStreamIds = await _blockScanAppService.GetMessageStreamIdsAsync(clientId, version);
        foreach (var streamId in messageStreamIds)
        {
            var stream =
                _clusterClient
                    .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                    .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

            var subscriptionHandles = await stream.GetAllSubscriptionHandles();
            if (!subscriptionHandles.IsNullOrEmpty())
            {
                subscriptionHandles.ForEach(async x =>
                    await x.ResumeAsync(HandleAsync));
            }
            else
            {
                await stream.SubscribeAsync(HandleAsync);
            }
        }
        
        await _blockScanAppService.StartScanAsync(clientId, version);
    }
    
    [HttpPost]
    [Route("upgrade")]
    //[Authorize]
    public virtual Task UpgradeVersionAsync(string clientId)
    {
        //var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
        return _blockScanAppService.UpgradeVersionAsync(clientId);
    }
    
    private async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken? token = null)
    {
        var clientVersion = await _blockScanAppService.GetClientVersionAsync(subscribedBlock.ClientId);
        if (subscribedBlock.Version != clientVersion.CurrentVersion &&
            subscribedBlock.Version != clientVersion.NewVersion)
        {
            return;
        }
        
        Console.WriteLine($"========= Version: {subscribedBlock.Version}");
    }
}