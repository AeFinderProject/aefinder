using AeFinder.App.Es;
using AeFinder.AppResources;
using AeFinder.AppResources.Dto;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.Options;
using AElf.EntityMapping.Elasticsearch;
using Elasticsearch.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nest;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AeFinder.BackgroundWorker.ScheduledTask;

public class AppResourceUsageWorker : AsyncPeriodicBackgroundWorkerBase, ISingletonDependency
{
    private readonly IAppResourceUsageService _appResourceUsageService;
    private readonly IElasticsearchClientProvider _elasticsearchClientProvider;
    private readonly IAppService _appService;
    private readonly AppResourceOptions _appResourceOptions;
    private readonly ScheduledTaskOptions _scheduledTaskOptions;

    public AppResourceUsageWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IAppResourceUsageService appResourceUsageService, IElasticsearchClientProvider elasticsearchClientProvider,
        IAppService appService, IOptionsSnapshot<AppResourceOptions> appResourceOptions,
        IOptionsSnapshot<ScheduledTaskOptions> scheduledTaskOptions)
        : base(timer, serviceScopeFactory)
    {
        _appResourceUsageService = appResourceUsageService;
        _elasticsearchClientProvider = elasticsearchClientProvider;
        _appService = appService;
        _scheduledTaskOptions = scheduledTaskOptions.Value;
        _appResourceOptions = appResourceOptions.Value;
        
        Timer.Period = _scheduledTaskOptions.AppResourceUsageTaskPeriodMilliSeconds;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var client = _elasticsearchClientProvider.GetClient();
        var catResponse = await client.Cat.IndicesAsync(r => r.Bytes(Bytes.Mb));

        var indices = new Dictionary<string, List<CatIndicesRecord>>();
        foreach (var record in catResponse.Records)
        {
            var appId = record.Index.Split('-')[0];
            if (!indices.TryGetValue(appId, out var records))
            {
                records = new List<CatIndicesRecord>();
            }

            records.Add(record);
            indices[appId] = records;
        }

        var skipCount = 0;
        var maxResultCount = 100;
        var apps = await _appService.GetIndexListAsync(new GetAppInput
        {
            SkipCount = skipCount,
            MaxResultCount = maxResultCount
        });

        while (apps.Items.Count > 0)
        {
            foreach (var app in apps.Items)
            {
                if (!indices.TryGetValue(app.AppId, out var records))
                {
                    await _appResourceUsageService.DeleteAsync(app.AppId);
                }
                else
                {
                    var appResourceUsage = new AppResourceUsageDto
                    {
                        AppInfo = new AppInfoImmutable
                        {
                            AppId = app.AppId,
                            AppName = app.AppName
                        },
                        OrganizationId = Guid.Parse(app.OrganizationId),
                        ResourceUsages = new Dictionary<string, ResourceUsageDto>()
                    };

                    foreach (var record in records)
                    {
                        var version = record.Index.Split('.')[0].Split('-')[1];
                        if (!appResourceUsage.ResourceUsages.TryGetValue(version, out var resourceUsage))
                        {
                            resourceUsage = new ResourceUsageDto();
                        }

                        resourceUsage.StoreSize += Convert.ToDecimal(record.PrimaryStoreSize) *
                            _appResourceOptions.StoreReplicates / 1024;
                    }

                    await _appResourceUsageService.AddOrUpdateAsync(appResourceUsage);
                }
            }

            skipCount += maxResultCount;
            apps = await _appService.GetIndexListAsync(new GetAppInput
            {
                SkipCount = skipCount,
                MaxResultCount = maxResultCount
            });
        }
    }
}