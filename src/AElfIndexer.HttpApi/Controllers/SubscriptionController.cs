using System.Threading.Tasks;
using AElfIndexer.BlockScan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Volo.Abp;

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
    
    [HttpPost]
    [Authorize]
    public virtual Task<string> SubmitSubscriptionInfoAsync(Subscription subscriptionInfos)
    {
        return _blockScanAppService.SubmitSubscriptionInfoAsync(ClientId,subscriptionInfos);
    }

    // [HttpPut("{Version}")]
    // [Authorize]
    // public virtual Task UpdateSubscriptionInfoAsync(string Version, [FromBody]List<SubscriptionInfo> subscriptionInfos)
    // {
    //     return _blockScanAppService.UpdateSubscriptionInfoAsync(ClientId, Version, subscriptionInfos);
    // }

    [HttpGet]
    [Authorize]
    public virtual Task<SubscriptionInfoDto> GetSubscriptionInfoAsync()
    {
        return _blockScanAppService.GetSubscriptionInfoAsync(ClientId);
    }
}