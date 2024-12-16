using System;
using System.Collections.Generic;
using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.CodeOps;
using AeFinder.Grains.Grain.ApiKeys;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Logger;
using AeFinder.Logger.Entities;
using AeFinder.Metrics;
using AeFinder.Orleans.TestBase;
using AElf.EntityMapping.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nest;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AeFinder;

[DependsOn(
    typeof(AeFinderApplicationModule),
    typeof(AeFinderDomainTestModule),
    typeof(AeFinderOrleansTestBaseModule),
    typeof(AElfEntityMappingElasticsearchModule)
)]
public class AeFinderApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<ApiOptions>(o =>
        {
            o.BlockQueryHeightInterval = 1000;
            o.TransactionQueryHeightInterval = 1000;
            o.LogEventQueryHeightInterval = 1000;
            o.MaxQuerySize = 10;
        });
        
        context.Services.AddTransient<ICodeAuditor>(o=>Mock.Of<ICodeAuditor>());
        
        context.Services.Configure<BlockPushOptions>(o =>
        {
            o.MessageStreamNamespaces = new List<string> { "MessageStreamNamespace" };
        });
        context.Services.AddTransient<IAppResourceLimitProvider, AppResourceLimitProvider>();
        context.Services.AddTransient<IKubernetesAppMonitor, DefaultKubernetesAppMonitor>();
        context.Services.AddTransient<ILogService, LogElasticSearchService>();
        context.Services.AddSingleton<ElasticClient>(provider =>
        {
            var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
                .EnableHttpCompression();
            return new ElasticClient(settings);
        });
        context.Services.Configure<LogElasticSearchOptions>(options =>
        {
            options.Uris = new List<string>(){"http://localhost:9200"};
        });
        context.Services.Configure<ApiKeyOptions>(o =>
        {
            o.IgnoreKeys = new HashSet<string> { "app" };
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var elasticClient = context.ServiceProvider.GetRequiredService<ElasticClient>();
        
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var elasticClient = context.ServiceProvider.GetRequiredService<ElasticClient>();
        elasticClient.Indices.Delete("aefinder-app-testindexer*");
    }
}
