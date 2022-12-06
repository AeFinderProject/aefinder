using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Block;
using AElfIndexer.Block.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfIndexer.Controllers;


[RemoteService]
[ControllerName("Block")]
[Route("api/app/block")]
public class BlockController : AbpController
{
    private readonly IBlockAppService _blockAppService;

    public BlockController(IBlockAppService blockAppService)
    {
        _blockAppService = blockAppService;
    }
    
    [HttpPost]
    [Route("blocks")]
    [Authorize]
    public virtual Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input)
    {
        return _blockAppService.GetBlocksAsync(input);
    }
    
    [HttpPost]
    [Route("transactions")]
    [Authorize]
    public virtual Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        return _blockAppService.GetTransactionsAsync(input);
    }
    
    [HttpPost]
    [Route("logevents")]
    [Authorize]
    public virtual Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input)
    {
        return _blockAppService.GetLogEventsAsync(input);
    }
}