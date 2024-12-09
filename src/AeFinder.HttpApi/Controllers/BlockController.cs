using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.ApiKeys;
using AeFinder.Block;
using AeFinder.Block.Dtos;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Timing;

namespace AeFinder.Controllers;

[RemoteService]
[ControllerName("Block")]
public class BlockController : AeFinderController
{
    private readonly IBlockAppService _blockAppService;
    private readonly IApiKeyService _apiKeyService;
    private readonly IClock _clock;

    public BlockController(IBlockAppService blockAppService, IApiKeyService apiKeyService, IClock clock)
    {
        _blockAppService = blockAppService;
        _apiKeyService = apiKeyService;
        _clock = clock;
    }

    [HttpPost]
    [Route("api/{key}/block/blocks")]
    public virtual async Task<List<BlockDto>> GetBlocksAsync(string key, GetBlocksInput input)
    {
        await _apiKeyService.IncreaseQueryBasicApiCountAsync(key, BasicApi.Block, GetOriginHost(), _clock.Now);
        return await _blockAppService.GetBlocksAsync(input);
    }

    [HttpPost]
    [Route("api/{key}/block/transactions")]
    public virtual async Task<List<TransactionDto>> GetTransactionsAsync(string key, GetTransactionsInput input)
    {
        await _apiKeyService.IncreaseQueryBasicApiCountAsync(key, BasicApi.Transaction, GetOriginHost(), _clock.Now);
        return await _blockAppService.GetTransactionsAsync(input);
    }

    [HttpPost]
    [Route("api/{key}/block/logevents")]
    public virtual async Task<List<LogEventDto>> GetLogEventsAsync(string key, GetLogEventsInput input)
    {
        await _apiKeyService.IncreaseQueryBasicApiCountAsync(key, BasicApi.LogEvent, GetOriginHost(), _clock.Now);
        return await _blockAppService.GetLogEventsAsync(input);
    }
    
    [HttpPost]
    [Route("api/app/block/summaries")]
    public virtual Task<List<SummaryDto>> GetSummariesAsync(GetSummariesInput input)
    {
        return _blockAppService.GetSummariesAsync(input);
    }
}