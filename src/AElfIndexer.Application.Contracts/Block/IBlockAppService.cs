using System.Collections.Generic;
using System.Threading.Tasks;
using AElfIndexer.Block.Dtos;
using Volo.Abp.Application.Services;

namespace AElfIndexer.Block;

public interface IBlockAppService:IApplicationService
{
    Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input);
    Task<long> GetBlockCountAsync(GetBlocksInput input);
    Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input);
    Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input);
}