using System.Threading.Tasks;
using AElf.EntityMapping.Elasticsearch.Services;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Commons;

public class ElasticSearchCommonService: IElasticSearchCommonService, ISingletonDependency
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