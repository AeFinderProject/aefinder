using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScan.AElf;
using AElfScan.AElf.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AElfScan.Controllers.AElf;


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
    
    [HttpGet]
    [Route("transactions")]
    public virtual Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input)
    {
        return _blockAppService.GetTransactionsAsync(input);
    }
}