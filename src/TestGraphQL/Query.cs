using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Volo.Abp.ObjectMapping;

namespace GraphQL;

public class Query
{
    public static string Ali() => "Ali BB";
    
    public static Task<TestBlock> Block([FromServices] IBlockService blockService, string id)
        => blockService.GetBlockAsync(id);
    
    public static async Task<TestBlock> BlockTest([FromServices] IAElfIndexerClientEntityRepository<TestBlockIndex,string,BlockInfo,Query> repository, [FromServices] IObjectMapper objectMapper, string id)
    {
        var block = await repository.GetAsync(id);
        return objectMapper.Map<TestBlockIndex,TestBlock>(block);
    }
}