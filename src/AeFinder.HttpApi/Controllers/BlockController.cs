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
    
    [HttpPost]
    [Route("summaries")]
    [Authorize]
    public virtual Task<List<SummaryDto>> GetSummariesAsync(GetSummariesInput input)
    {
        return _blockAppService.GetSummariesAsync(input);
    }
    
    [HttpPost]
    [Route("blocks/route-key-alias")]
    [Authorize]
    public virtual Task<List<BlockDto>> GetBlocksByRouteKeyWithIndexAliasAsync(GetBlocksInput input)
    {
        return _blockAppService.GetBlocksByRouteKeyWithIndexAliasAsync(input);
    }
    
    [HttpPost]
    [Route("blocks/route-key")]
    [Authorize]
    public virtual Task<List<BlockDto>> GetBlocksByRouteKeyAsync(GetBlocksInput input)
    {
        return _blockAppService.GetBlocksByRouteKeyAsync(input);
    }
    
    [HttpPost]
    [Route("transactions/route-key-alias")]
    [Authorize]
    public virtual Task<List<TransactionDto>> GetTransactionsByRouteKeyWithIndexAliasAsync(GetTransactionsInput input)
    {
        return _blockAppService.GetTransactionsByRouteKeyWithIndexAliasAsync(input);
    }
    
    [HttpPost]
    [Route("transactions/route-key")]
    [Authorize]
    public virtual Task<List<TransactionDto>> GetTransactionsByRouteKeyAsync(GetTransactionsInput input)
    {
        return _blockAppService.GetTransactionsByRouteKeyAsync(input);
    }
    
    [HttpPost]
    [Route("logevents/route-key-alias")]
    [Authorize]
    public virtual Task<List<LogEventDto>> GetLogEventsByRouteKeyWithIndexAliasAsync(GetLogEventsInput input)
    {
        return _blockAppService.GetLogEventsByRouteKeyWithIndexAliasAsync(input);
    }
    
    [HttpPost]
    [Route("logevents/route-key")]
    [Authorize]
    public virtual Task<List<LogEventDto>> GetLogEventsByRouteKeyAsync(GetLogEventsInput input)
    {
        return _blockAppService.GetLogEventsByRouteKeyAsync(input);
    }
}