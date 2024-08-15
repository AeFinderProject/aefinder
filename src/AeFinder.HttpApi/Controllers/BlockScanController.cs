using System.Linq;
using System.Threading.Tasks;
using AeFinder.Apps;
using AeFinder.BlockScan;
using AeFinder.Models;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nito.AsyncEx;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("BlockScan")]
[Route("api/block-scan")]
public class BlockScanController : AeFinderController
{
    private readonly IBlockScanAppService _blockScanAppService;
    private readonly IAppService _appService;

    public BlockScanController(IBlockScanAppService blockScanAppService, IAppService appService)
    {
        _blockScanAppService = blockScanAppService;
        _appService = appService;
    }

    [HttpPost]
    [Route("pause")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public virtual async Task PauseAsync(AppVersionInput input)
    {
        await _blockScanAppService.PauseAsync(input.AppId, input.Version);
    }
    
    [HttpPost]
    [Route("batch-pause")]
    [Authorize(Policy = "OnlyAdminAccess")]
    public async Task BatchPauseAsync(AppIdsInput input)
    {
        var tasks = input.AppIds.Select(async appId =>
        {
            var app = await _appService.GetIndexAsync(appId);

            if (app.Versions.PendingVersion != null)
            {
                await _blockScanAppService.PauseAsync(appId, app.Versions.PendingVersion);
            }

            if (app.Versions.CurrentVersion != null)
            {
                await _blockScanAppService.PauseAsync(appId, app.Versions.CurrentVersion);
            }
        });

        await tasks.WhenAll();
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