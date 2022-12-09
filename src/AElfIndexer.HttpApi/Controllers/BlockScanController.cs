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
[ControllerName("BlockScan")]
[Route("api/app/block-scan")]
public class BlockScanController : AbpController
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly ISubscribedBlockHandler _subscribedBlockHandler;

    public BlockScanController(IBlockScanAppService blockScanAppService, ISubscribedBlockHandler subscribedBlockHandler)
    {
        _blockScanAppService = blockScanAppService;
        _subscribedBlockHandler = subscribedBlockHandler;
    }
    
    [HttpPost]
    [Route("submit-subscribe-info")]
    [Authorize]
    public virtual Task<string> SubscribeAsync(List<SubscribeInfo> subscribeInfos)
    {
        var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
        return _blockScanAppService.SubmitSubscribeInfoAsync(clientId,subscribeInfos);
    }
    
    [HttpPost]
    [Route("start")]
    [Authorize]
    public virtual Task StartScanAsync(string version)
    {
        var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
        return _blockScanAppService.StartScanAsync(clientId, version);
    }
    
    [HttpPost]
    [Route("upgrade")]
    [Authorize]
    public virtual Task UpgradeVersionAsync()
    {
        var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
        return _blockScanAppService.UpgradeVersionAsync(clientId);
    }


    public static async Task HandleAsync(SubscribedBlockDto subscribedBlock, StreamSequenceToken? token = null)
    {
        Console.WriteLine($"========= Version: {subscribedBlock.Version}");
    }
}