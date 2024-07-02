using System.Threading.Tasks;
using AeFinder.BlockScan;
using AeFinder.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("BlockScan")]
[Route("api/block-scan")]
public class BlockScanController : AeFinderController
{
    private readonly IBlockScanAppService _blockScanAppService;

    public BlockScanController(IBlockScanAppService blockScanAppService)
    {
        _blockScanAppService = blockScanAppService;
    }

    [HttpPost]
    [Route("pause")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task PauseAsync(AppVersionInput input)
    {
        await _blockScanAppService.PauseAsync(input.AppId, input.Version);
    }
    
    [HttpPost]
    [Route("stop")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task StopAsync(AppVersionInput input)
    {
        await _blockScanAppService.StopAsync(input.AppId, input.Version);
    }

    [HttpPost]
    [Route("upgrade")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task UpgradeVersionAsync(AppInput input)
    {
        await _blockScanAppService.UpgradeVersionAsync(input.AppId);
    }
}