using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Block;
using AeFinder.Block.Dtos;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Block")]
public class BlockController : AbpController
{
    private readonly IBlockAppService _blockAppService;

    public BlockController(IBlockAppService blockAppService)
    {
        _blockAppService = blockAppService;
    }

    [HttpPost]
    [Route("api/{key}/block/blocks")]
    [Authorize]
    public virtual Task<List<BlockDto>> GetBlocksAsync(string key, GetBlocksInput input)
    {
        return _blockAppService.GetBlocksAsync(input);
    }

    [HttpPost]
    [Route("api/{key}/block/transactions")]
    [Authorize]
    public virtual Task<List<TransactionDto>> GetTransactionsAsync(string key, GetTransactionsInput input)
    {
        return _blockAppService.GetTransactionsAsync(input);
    }

    [HttpPost]
    [Route("api/{key}/block/logevents")]
    [Authorize]
    public virtual Task<List<LogEventDto>> GetLogEventsAsync(string key, GetLogEventsInput input)
    {
        return _blockAppService.GetLogEventsAsync(input);
    }
    
    [HttpPost]
    [Route("api/app/block/summaries")]
    [Authorize]
    public virtual Task<List<SummaryDto>> GetSummariesAsync(GetSummariesInput input)
    {
        return _blockAppService.GetSummariesAsync(input);
    }
}