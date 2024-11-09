using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.ApiTraffic;
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
    private readonly IApiTrafficService _apiTrafficService;

    public BlockController(IBlockAppService blockAppService, IApiTrafficService apiTrafficService)
    {
        _blockAppService = blockAppService;
        _apiTrafficService = apiTrafficService;
    }

    [HttpPost]
    [Route("api/{key}/block/blocks")]
    [Authorize]
    public virtual async Task<List<BlockDto>> GetBlocksAsync(string key, GetBlocksInput input)
    {
        await _apiTrafficService.IncreaseRequestCountAsync(key);
        return await _blockAppService.GetBlocksAsync(input);
    }

    [HttpPost]
    [Route("api/{key}/block/transactions")]
    [Authorize]
    public virtual async Task<List<TransactionDto>> GetTransactionsAsync(string key, GetTransactionsInput input)
    {
        await _apiTrafficService.IncreaseRequestCountAsync(key);
        return await _blockAppService.GetTransactionsAsync(input);
    }

    [HttpPost]
    [Route("api/{key}/block/logevents")]
    [Authorize]
    public virtual async Task<List<LogEventDto>> GetLogEventsAsync(string key, GetLogEventsInput input)
    {
        await _apiTrafficService.IncreaseRequestCountAsync(key);
        return await _blockAppService.GetLogEventsAsync(input);
    }
    
    [HttpPost]
    [Route("api/app/block/summaries")]
    [Authorize]
    public virtual Task<List<SummaryDto>> GetSummariesAsync(GetSummariesInput input)
    {
        return _blockAppService.GetSummariesAsync(input);
    }
}