using System.Threading.Tasks;
using AElfIndexer.BlockScan;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfIndexer.Controllers;

[RemoteService]
[ControllerName("BlockScan")]
[Route("api/app/block-scan")]
public class BlockScanController: AbpController
{
    private readonly IBlockScanAppService _blockScanAppService;

    public BlockScanController(IBlockScanAppService blockScanAppService)
    {
        _blockScanAppService = blockScanAppService;
    }

    [HttpPost]
    [Route("stop")]
    //[Authorize]
    public virtual Task StopAsync(string clientId, string version)
    {
        return _blockScanAppService.StopAsync(clientId,version);
    }
}