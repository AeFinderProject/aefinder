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

    public SubscriptionController(IBlockScanAppService blockScanAppService, IClusterClient clusterClient) : base(clusterClient)
    {
        _blockScanAppService = blockScanAppService;
    }

    [HttpPost]
    [Authorize]
    public virtual async Task<string> SubmitSubscriptionInfoAsync(SubscriptionManifestDto input)
    {
        return await _blockScanAppService.AddSubscriptionAsync(await GetAppId(), input);
    }

    // [HttpPut("{Version}")]
    // [Authorize]
    // public virtual Task UpdateSubscriptionInfoAsync(string Version, [FromBody]List<SubscriptionInfo> subscriptionInfos)
    // {
    //     return _blockScanAppService.UpdateSubscriptionInfoAsync(ClientId, Version, subscriptionInfos);
    // }

    [HttpGet]
    [Authorize]
    public virtual async Task<AllSubscriptionDto> GetSubscriptionInfoAsync()
    {
        return await _blockScanAppService.GetSubscriptionAsync(await GetAppId());
    }
}