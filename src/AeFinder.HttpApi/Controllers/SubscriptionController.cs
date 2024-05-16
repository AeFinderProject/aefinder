using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Studio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Subscription")]
[Route("api/app/subscription")]
public class SubscriptionController : AeFinderController
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IStudioService _studioService;

    public SubscriptionController(IBlockScanAppService blockScanAppService, IStudioService studioService)
    {
        _blockScanAppService = blockScanAppService;
        _studioService = studioService;
    }

    [HttpPost]
    [Authorize]
    public virtual async Task<string> SubmitSubscriptionInfoAsync(SubscriptionManifestDto input)
    {
        var appId = await _studioService.GetAppIdAsync();
        return await _blockScanAppService.AddSubscriptionAsync(appId, input);
    }
    
    [HttpPut("{Version}")]
    [Authorize]
    public virtual async Task UpdateSubscriptionInfoAsync(string Version, [FromBody]SubscriptionManifestDto subscriptionManifest)
    {
        var appId = await _studioService.GetAppIdAsync();
        await _blockScanAppService.UpdateSubscriptionInfoAsync(appId, Version, subscriptionManifest);
    }

    [HttpGet]
    [Authorize]
    public virtual async Task<AllSubscriptionDto> GetSubscriptionInfoAsync()
    {
        var appId = await _studioService.GetAppIdAsync();
        return await _blockScanAppService.GetSubscriptionAsync(appId);
    }
}