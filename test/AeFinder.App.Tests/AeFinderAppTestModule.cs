using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AeFinder.App.BlockChain;
using AeFinder.App.BlockState;
using AeFinder.App.MockApp;
using AeFinder.App.OperationLimits;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Orleans.TestBase;
using AeFinder.Sdk.Entities;
using AeFinder.Sdk.Processor;
using AElf.EntityMapping.Elasticsearch;
using AElf.EntityMapping.Elasticsearch.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AeFinder.App;

[DependsOn(
    typeof(AeFinderAppModule),
    typeof(AeFinderDomainTestModule),
    typeof(AeFinderOrleansTestBaseModule)
)]
public class AeFinderAppTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderAppTestModule>(); });
        
        
        context.Services.Configure<AppStateOptions>(o =>
        {
            o.AppDataCacheCount = 5;
        });
        context.Services.Configure<ChainNodeOptions>(o =>
        {
            o.ChainNodes = new Dictionary<string, string>()
            {
                { "AELF", "http://mainchain.io" },
                { "tDVV", "http://sidechain.io" }
            };
        });
        context.Services.Configure<AppInfoOptions>(o =>
        {
            o.AppId = "TestAppId";
            o.Version= "TestVersion";
            o.ClientType = ClientType.Query;
        });
        context.Services.Configure<OperationLimitOptions>(o =>
        {
            o.MaxContractCallCount = 3;
            o.MaxEntitySize = 10000;
            o.MaxEntityCallCount = 100;
            o.MaxLogSize = 10;
            o.MaxLogCallCount = 3;
        });
        
        context.Services.AddTransient<ILogEventProcessor, TokenTransferredProcessor>();
    }
    
    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(async () =>
            await CreateIndexAsync(context.ServiceProvider)
        );
    }

    private async Task CreateIndexAsync(IServiceProvider serviceProvider)
    {
        var appInfoOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<AppInfoOptions>>().Value;
        var types = GetTypesAssignableFrom<IAeFinderEntity>(typeof(AeFinderAppTestModule).Assembly);
        var elasticIndexService = serviceProvider.GetRequiredService<IElasticIndexService>();
        foreach (var t in types)
        {
            var indexName = $"{appInfoOptions.AppId}-{appInfoOptions.Version}.{t.Name}".ToLower();
            await elasticIndexService.CreateIndexAsync(indexName, t);
        }
    }

    private List<Type> GetTypesAssignableFrom<T>(Assembly assembly)
    {
        var compareType = typeof(T);
        return assembly.DefinedTypes
            .Where(type => compareType.IsAssignableFrom(type) && !compareType.IsAssignableFrom(type.BaseType) &&
                           !type.IsAbstract && type.IsClass && compareType != type)
            .Cast<Type>().ToList();
    }
    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var appInfoOptions = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<AppInfoOptions>>().Value;
        var clientProvider = context.ServiceProvider.GetRequiredService<IElasticsearchClientProvider>();
        var client = clientProvider.GetClient();
        var prefix = $"{appInfoOptions.AppId}-{appInfoOptions.Version}".ToLower();
        
        client.Indices.Delete(prefix+"*");
        client.Indices.DeleteTemplate(prefix + "*");
    }
}