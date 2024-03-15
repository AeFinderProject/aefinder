using System.Threading.Tasks;
using AeFinder.BlockScan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("BlockScan")]
[Route("api/app/block-scan")]
public class BlockScanController: AeFinderController
{
    private readonly IBlockScanAppService _blockScanAppService;

    public BlockScanController(IBlockScanAppService blockScanAppService)
    {
        _blockScanAppService = blockScanAppService;
    }

    [HttpPost]
    [Route("pause")]
    [Authorize]
    public virtual Task PauseAsync(string version)
    {
        return _blockScanAppService.PauseAsync(ClientId, version);
    }

    [HttpPost]
    [Route("stop")]
    [Authorize]
    public virtual Task StopAsync(string version)
    {
        return _blockScanAppService.StopAsync(ClientId,version);
    }
    
    [HttpPost]
    [Route("upgrade")]
    [Authorize]
    public virtual Task UpgradeVersionAsync()
    {
        return _blockScanAppService.UpgradeVersionAsync(ClientId);
    }
}