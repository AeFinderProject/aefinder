using System.Threading.Tasks;
using AeFinder.Commons;
using AElf.EntityMapping.Elasticsearch.Services;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Common;

public class ElasticSearchCommonService : IElasticSearchCommonService, ISingletonDependency
{
    private readonly IElasticIndexService _elasticIndexService;

    public ElasticSearchCommonService(IElasticIndexService elasticIndexService)
    {
        _elasticIndexService = elasticIndexService;
    }

    public async Task DeleteAppIndexAsync(string indexName)
    {
        await _elasticIndexService.DeleteIndexAsync(indexName);
    }
}