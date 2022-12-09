using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElfIndexer.Block;
using AElfIndexer.Block.Dtos;
using AElfIndexer.BlockScan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfIndexer.Controllers;

[RemoteService]
[ControllerName("BlockScan")]
[Route("api/app/block-scan")]
public class BlockScanController : AbpController
{
    private readonly IBlockScanAppService _blockScanAppService;

    public BlockScanController(IBlockScanAppService blockScanAppService)
    {
        _blockScanAppService = blockScanAppService;
    }
    
    [HttpPost]
    [Route("submit-subscribe-info")]
    [Authorize]
    public virtual Task SubscribeAsync(List<SubscribeInfo> subscribeInfos)
    {
        var clientId = CurrentUser.GetAllClaims().First(o => o.Type == "client_id").Value;;
        return _blockScanAppService.SubmitSubscribeInfoAsync(clientId,subscribeInfos);
    }
}