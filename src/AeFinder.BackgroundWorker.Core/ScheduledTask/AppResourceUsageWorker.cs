using AeFinder.App.Es;
using AeFinder.AppResources;
using AeFinder.AppResources.Dto;
using AeFinder.Apps;
using AeFinder.Assets;
using AeFinder.BackgroundWorker.Options;
using AeFinder.Merchandises;
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
    private readonly IAssetService _assetService;

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
        var indices = await GetIndicesRecordsAsync();
        
        var skipCount = 0;
        var maxResultCount = 100;
        var apps = await _appService.GetIndexListAsync(new GetAppInput
        {
            SkipCount = skipCount,
            MaxResultCount = maxResultCount
        });

        while (apps.Items.Count > 0)
        {
            var toAddAppResourceUsage = new List<AppResourceUsageDto>();
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
                        ResourceUsages = new Dictionary<string, List<ResourceUsageDto>>()
                    };

                    var storeSizes = new Dictionary<string, decimal>();
                    foreach (var record in records)
                    {
                        var version = record.Index.Split('.')[0].Split('-')[1];
                        if (!storeSizes.TryGetValue(version, out var usage))
                        {
                            usage = 0;
                        }

                        usage += Convert.ToDecimal(record.PrimaryStoreSize) * _appResourceOptions.StoreReplicates /
                                 1024;
                        storeSizes[version] = usage;
                    }
                    
                    var storageLimit = await GetStorageLimitAsync(Guid.Parse(app.OrganizationId), app.AppId);

                    foreach (var storeSize in storeSizes)
                    {
                        if (!appResourceUsage.ResourceUsages.TryGetValue(storeSize.Key, out var resourceUsage))
                        {
                            resourceUsage = new List<ResourceUsageDto>();
                        }

                        resourceUsage.Add(new ResourceUsageDto
                        {
                            Name = AeFinderApplicationConsts.AppStorageResourceName,
                            Limit = storageLimit.ToString(),
                            Usage = storeSize.Value.ToString("F2")
                        });
                        appResourceUsage.ResourceUsages[storeSize.Key] = resourceUsage;
                    }
                    
                    toAddAppResourceUsage.Add(appResourceUsage);
                }

                if (toAddAppResourceUsage.Count > 0)
                {
                    await _appResourceUsageService.AddOrUpdateAsync(toAddAppResourceUsage);
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

    private async Task<Dictionary<string, List<CatIndicesRecord>>> GetIndicesRecordsAsync()
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

        return indices;
    }

    private async Task<decimal> GetStorageLimitAsync(Guid organizationId, string appId)
    {
        var storageAssets = await _assetService.GetListAsync(organizationId, new GetAssetInput()
        {
            Type = MerchandiseType.Storage,
            AppId = appId,
            IsDeploy = true
        });
        var storageAsset = storageAssets.Items.FirstOrDefault();
        return storageAsset?.Replicas ?? 0;
    }
}