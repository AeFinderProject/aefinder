using AeFinder.Client.Providers;
using AElf.Indexing.Elasticsearch;

namespace GraphQL;

public class TestMutation
{
    public static async Task<string> AddBlock([FromServices] INESTRepository<TestBlockIndex,string> repository, [FromServices] IAeFinderClientInfoProvider provider, TestBlockIndex block)
    {
        await repository.AddOrUpdateAsync(block,
            $"{provider.GetClientId()}_{provider.GetVersion()}.{nameof(TestBlockIndex)}".ToLower());
        return "success";
    }
}