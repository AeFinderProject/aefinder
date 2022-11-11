using System.Collections.Generic;
using System.Threading.Tasks;
using AElfScan.Block.Dtos;
using Volo.Abp.Application.Services;

namespace AElfScan.Block;

public interface IBlockAppService:IApplicationService
{
    Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input);
    Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input);
    Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input);
}