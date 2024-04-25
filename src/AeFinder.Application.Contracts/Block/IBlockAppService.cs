using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Block.Dtos;
using Volo.Abp.Application.Services;

namespace AeFinder.Block;

public interface IBlockAppService:IApplicationService
{
    Task<List<BlockDto>> GetBlocksAsync(GetBlocksInput input);
    Task<long> GetBlockCountAsync(GetBlocksInput input);
    Task<List<TransactionDto>> GetTransactionsAsync(GetTransactionsInput input);
    Task<List<LogEventDto>> GetLogEventsAsync(GetLogEventsInput input);
    
    Task<List<TransactionDto>> GetSubscriptionTransactionsAsync(GetSubscriptionTransactionsInput input);

    Task<List<SummaryDto>> GetSummariesAsync(GetSummariesInput input);
}