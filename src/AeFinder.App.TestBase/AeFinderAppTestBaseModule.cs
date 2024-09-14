using AeFinder.App.BlockChain;
using AeFinder.App.BlockState;
using AeFinder.BlockScan;
using AeFinder.User;
using AeFinder.User.Dto;
using AElf.EntityMapping.Elasticsearch;
using AElf.EntityMapping.Elasticsearch.Options;
using AElf.EntityMapping.Elasticsearch.Services;
using AElf.EntityMapping.Options;
using Elasticsearch.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Orleans;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AeFinder.App.TestBase;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpTestBaseModule),
    typeof(AbpEventBusModule),
    typeof(AeFinderAppModule)
)]
public class AeFinderAppTestBaseModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpBackgroundJobOptions>(options => { options.IsJobExecutionEnabled = false; });

        context.Services.AddSingleton<ClusterFixture>();
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetRequiredService<ClusterFixture>().Cluster.Client);
        context.Services.RemoveAll(o => o.ImplementationType == typeof(AeFinder.App.BlockState.AppDataIndexProvider<>));
        context.Services.AddSingleton(typeof(IAppDataIndexProvider<>), typeof(AppDataIndexProvider<>));

        context.Services.Configure<ChainNodeOptions>(o =>
        {
            o.ChainNodes = new Dictionary<string, string>()
            {
                { "AELF", "" }
            };
        });

        context.Services.Configure<AElfEntityMappingOptions>(options => { options.CollectionPrefix = "AeFinderTest"; });

        context.Services.Configure<ElasticsearchOptions>(options =>
        {
            options.NumberOfReplicas = 0;
            options.NumberOfShards = 1;
            options.Refresh = Refresh.True;
        });

        context.Services.Configure<AppInfoOptions>(o =>
        {
            o.AppId = "AppId";
            o.Version = "Version";
            o.ClientType = ClientType.Query;
        });

        var applicationBuilder = new ApplicationBuilder(context.Services.BuildServiceProvider());
        context.Services.AddObjectAccessor<IApplicationBuilder>(applicationBuilder);
        var mockBlockScanAppService = new Mock<IBlockScanAppService>();
        mockBlockScanAppService.Setup(p => p.GetMessageStreamIdsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.FromResult(new List<Guid>()));
        context.Services.AddSingleton(mockBlockScanAppService.Object);
        
        
    }

    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(async () =>
            await CreateIndexAsync(context.ServiceProvider)
        );
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        AsyncHelper.RunSync(async () =>
            await DeleteIndexAsync(context.ServiceProvider)
        );
    }

    private async Task CreateIndexAsync(IServiceProvider serviceProvider)
    {
        var entityTypesOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<AeFinderAppEntityOptions>>().Value;
        var elasticsearchOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<ElasticsearchOptions>>().Value;

        var elasticIndexService = serviceProvider.GetRequiredService<IElasticIndexService>();
        foreach (var t in entityTypesOptions.EntityTypes)
        {
            var indexName = GetIndexName(serviceProvider, t);
            await elasticIndexService.CreateIndexAsync(indexName, t, elasticsearchOptions.NumberOfShards,
                elasticsearchOptions.NumberOfReplicas);
        }
    }

    private async Task DeleteIndexAsync(IServiceProvider serviceProvider)
    {
        var entityTypesOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<AeFinderAppEntityOptions>>().Value;
        var clientProvider = serviceProvider.GetRequiredService<IElasticsearchClientProvider>();
        var client = clientProvider.GetClient();

        foreach (var indexName in entityTypesOptions.EntityTypes.Select(t => GetIndexName(serviceProvider, t)))
        {
            await client.Indices.DeleteAsync(indexName);
        }
    }

    private string GetIndexName(IServiceProvider serviceProvider, Type type)
    {
        var appInfoOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<AppInfoOptions>>().Value;
        return $"{appInfoOptions.AppId}-{appInfoOptions.Version}.{type.Name}".ToLower();
    }
}