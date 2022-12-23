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
public class SubscriptionController : AElfIndexerController
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IClusterClient _clusterClient;

    public SubscriptionController(IBlockScanAppService blockScanAppService, IClusterClient clusterClient)
    {
        _blockScanAppService = blockScanAppService;
        _clusterClient = clusterClient;
    }
    
    [HttpPut]
    [Authorize]
    public virtual Task<string> SubmitSubscriptionInfoAsync(List<SubscriptionInfo> subscriptionInfos)
    {
        return _blockScanAppService.SubmitSubscriptionInfoAsync(ClientId,subscriptionInfos);
    }
    
    [HttpGet]
    [Authorize]
    public virtual Task<SubscriptionInfoDto> GetSubscriptionInfoAsync()
    {
        return _blockScanAppService.GetSubscriptionInfoAsync(ClientId);
    }
}