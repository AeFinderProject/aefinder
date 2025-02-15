using System.Linq;
using System.Threading.Tasks;
using AeFinder.App.Es;
using AElf.EntityMapping.Elasticsearch;
using AElf.EntityMapping.Repositories;
using Elasticsearch.Net;
using Elasticsearch.Net.Specification.CatApi;
using Nest;
using Shouldly;
using Xunit;

namespace AeFinder.AppResources;

public class AppResourceUsageServiceTests : AeFinderApplicationAppTestBase
{
    private readonly IEntityMappingRepository<AppResourceUsageIndex, string> _entityMappingRepository;
    private readonly IElasticsearchClientProvider _elasticsearchClientProvider;

    public AppResourceUsageServiceTests()
    {
        _elasticsearchClientProvider = GetRequiredService<IElasticsearchClientProvider>();
        _entityMappingRepository = GetRequiredService<IEntityMappingRepository<AppResourceUsageIndex, string>>();
    }

    [Fact]
    public async Task GetTest()
    {
        var client = _elasticsearchClientProvider.GetClient();

        var index = client.LowLevel.Cat.Indices<StringResponse>(new CatIndicesRequestParameters
        {
            Format = "json",
            Headers = new string[]
            {
                "index",
                "store.size",
                "pri.store.size"
            },
            Bytes = Bytes.Mb
        });

        var index2 = await client.Cat.IndicesAsync(r => r.Bytes(Bytes.Kb));
        ;
        ;
    }
}