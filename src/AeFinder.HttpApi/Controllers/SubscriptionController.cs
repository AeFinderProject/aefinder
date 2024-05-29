using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Studio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Subscription")]
[Route("api/apps/subscriptions")]
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
    public async Task<string> AddSubscriptionAsync(SubscriptionManifestDto input)
    {
        var appId = ClientId;
        return await _blockScanAppService.AddSubscriptionAsync(appId, input);
    }
    
    [HttpPut]
    //[Authorize]
    [Route("manifest/{version}")]
    public async Task<string> UpdateManifestAsync()
    {
        return null;
    }
    
    [HttpPut]
    //[Authorize]
    [Route("code/{version}")]
    public async Task<string> UpdateCodeAsync()
    {
        return null;
    }

    [HttpGet]
    //[Authorize]
    public async Task<AllSubscriptionDto> GetSubscriptionInfoAsync()
    {
        var appId = ClientId;
        return await _blockScanAppService.GetSubscriptionAsync(appId);
    }
}