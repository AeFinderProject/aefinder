using AeFinder.AppResources;
using AElf.EntityMapping.Elasticsearch;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class AppResourceUsageWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly IAppResourceUsageService _appResourceUsageService;
    private readonly IElasticsearchClientProvider _elasticsearchClientProvider;

    public AppResourceUsageWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IAppResourceUsageService appResourceUsageService, IElasticsearchClientProvider elasticsearchClientProvider)
        : base(timer, serviceScopeFactory)
    {
        _appResourceUsageService = appResourceUsageService;
        _elasticsearchClientProvider = elasticsearchClientProvider;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var client = _elasticsearchClientProvider.GetClient();
        var indices = await client.Cat.IndicesAsync(r => r.Bytes(Bytes.Kb));
    }
    
}