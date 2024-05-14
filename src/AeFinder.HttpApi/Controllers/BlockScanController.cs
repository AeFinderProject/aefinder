using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Studio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("BlockScan")]
[Route("api/app/block-scan")]
public class BlockScanController : AeFinderController
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IStudioService _studioService;

    public BlockScanController(IBlockScanAppService blockScanAppService, IStudioService studioService)
    {
        _blockScanAppService = blockScanAppService;
        _studioService = studioService;
    }

    [HttpPost]
    [Route("pause")]
    [Authorize]
    public virtual async Task<Task> PauseAsync(string version)
    {
        var appId = await _studioService.GetAppIdAsync();
        return _blockScanAppService.PauseAsync(appId, version);
    }

    [HttpPost]
    [Route("stop")]
    [Authorize]
    public virtual async Task<Task> StopAsync(string version)
    {
        var appId = await _studioService.GetAppIdAsync();
        return  _blockScanAppService.StopAsync(appId, version);
    }

    [HttpPost]
    [Route("upgrade")]
    [Authorize]
    public virtual async Task<Task> UpgradeVersionAsync()
    {
        var appId = await _studioService.GetAppIdAsync();
        return _blockScanAppService.UpgradeVersionAsync(appId);
    }
}