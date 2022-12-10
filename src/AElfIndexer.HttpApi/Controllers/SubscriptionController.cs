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

    public SubscriptionController(IBlockScanAppService blockScanAppService)
    {
        _blockScanAppService = blockScanAppService;
    }
    
    [HttpPut]
    [Route("subscription")]
    [Authorize]
    public virtual Task<string> SubmitSubscribeInfoAsync(List<SubscribeInfo> subscribeInfos)
    {
        var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
        return _blockScanAppService.SubmitSubscribeInfoAsync(clientId,subscribeInfos);
    }
    
    // Test
    // [HttpPost]
    // [Route("start")]
    // [Authorize]
    // public virtual Task StartScanAsync(string version)
    // {
    //     var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
    //     return _blockScanAppService.StartScanAsync(clientId, version);
    // }
    //
    // [HttpPost]
    // [Route("upgrade")]
    // [Authorize]
    // public virtual Task UpgradeVersionAsync()
    // {
    //     var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
    //     return _blockScanAppService.UpgradeVersionAsync(clientId);
    // }
}