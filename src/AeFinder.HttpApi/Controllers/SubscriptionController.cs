using System.Threading.Tasks;
using AeFinder.BlockScan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Subscription")]
[Route("api/app/subscription")]
public class SubscriptionController : AeFinderController
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
    public virtual Task<string> SubmitSubscriptionInfoAsync(SubscriptionManifestDto input)
    {
        return _blockScanAppService.AddSubscriptionAsync(ClientId, input);
    }

    // [HttpPut("{Version}")]
    // [Authorize]
    // public virtual Task UpdateSubscriptionInfoAsync(string Version, [FromBody]List<SubscriptionInfo> subscriptionInfos)
    // {
    //     return _blockScanAppService.UpdateSubscriptionInfoAsync(ClientId, Version, subscriptionInfos);
    // }

    [HttpGet]
    [Authorize]
    public virtual Task<AllSubscriptionDto> GetSubscriptionInfoAsync()
    {
        return _blockScanAppService.GetSubscriptionAsync(ClientId);
    }
}