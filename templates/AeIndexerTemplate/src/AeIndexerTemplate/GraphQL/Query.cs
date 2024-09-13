using AeFinder.Sdk;
using AeIndexerTemplate.Entities;
using GraphQL;
using Volo.Abp.ObjectMapping;

namespace AeIndexerTemplate.GraphQL;

public class Query
{
    public static async Task<List<MyEntityDto>> MyEntity(
        [FromServices] IReadOnlyRepository<MyEntity> repository,
        [FromServices] IObjectMapper objectMapper,
        GetMyEntityInput input)
    {
        var queryable = await repository.GetQueryableAsync();
        
        queryable = queryable.Where(a => a.Metadata.ChainId == input.ChainId);
        
        if (!input.Address.IsNullOrWhiteSpace())
        {
            queryable = queryable.Where(a => a.Address == input.Address);
        }
        
        var accounts= queryable.ToList();

        return objectMapper.Map<List<MyEntity>, List<MyEntityDto>>(accounts);
    }
}