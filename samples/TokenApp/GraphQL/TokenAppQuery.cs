using AeFinder.Sdk;
using GraphQL;
using TokenApp.Entities;

namespace TokenApp.GraphQL;

public class TokenAppQuery
{
    public static async Task<List<Account>> Account([FromServices] IReadOnlyRepository<Account> repository,
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

        return queryable.ToList();
    }
}