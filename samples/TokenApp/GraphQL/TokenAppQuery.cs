using AeFinder.Sdk;
using GraphQL;
using TokenApp.Entities;
using Volo.Abp.ObjectMapping;

namespace TokenApp.GraphQL;

public class TokenAppQuery
{
    public static async Task<List<AccountDto>> Account(
        [FromServices] IReadOnlyRepository<Account> repository,
        [FromServices] IObjectMapper objectMapper,
        GetAccountInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        
        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        
        if (!input.Address.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(a => a.Address == input.Address);
        }
        
        if(!input.Symbol.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(a => a.Symbol == input.Symbol);
        }
        
        var accounts= queryable.ToList();

        return objectMapper.Map<List<Account>, List<AccountDto>>(accounts);
    }
    
    public static async Task<List<TransferRecordDto>> TransferRecord(
        [FromServices] IReadOnlyRepository<TransferRecord> repository,
        [FromServices] IObjectMapper objectMapper,
        GetTransferRecordInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        
        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        
        if (!input.Address.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(a => a.FromAddress == input.Address || a.ToAddress == input.Address);
        }
        
        if(!input.Symbol.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(a => a.Symbol == input.Symbol);
        }
        
        var accounts= queryable.ToList();

        return objectMapper.Map<List<TransferRecord>, List<TransferRecordDto>>(accounts);
    }
}