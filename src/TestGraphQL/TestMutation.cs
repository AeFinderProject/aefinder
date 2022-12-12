using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client.Providers;

namespace GraphQL;

public class TestMutation
{
    public static async Task<string> AddBlock([FromServices] INESTRepository<TestBlockIndex,string> repository, [FromServices] IAElfIndexerClientInfoProvider<Query> provider, TestBlockIndex block)
    {
        await repository.AddOrUpdateAsync(block,
            $"{provider.GetClientId()}_{provider.GetVersion()}.{nameof(TestBlockIndex)}".ToLower());
        return "success";
    }
}