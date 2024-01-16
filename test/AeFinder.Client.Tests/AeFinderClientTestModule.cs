using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AeFinder.Client.Handlers;
using AeFinder.Client.Providers;
using AeFinder.Grains.Grain.Client;
using AeFinder.Orleans.TestBase;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Services;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AeFinder.Client;

[DependsOn(
    typeof(AeFinderClientModule),
    typeof(AeFinderDomainTestModule),
    typeof(AeFinderOrleansTestBaseModule)
)]
public class AeFinderClientTestModule : AbpModule
{
    private string ClientId { get; } = "TestClient";
    private string Version { get; } = "TestVersion";

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderClientTestModule>(); });
        context.Services.AddSingleton<IAeFinderClientInfoProvider, AeFinderClientInfoProvider>();
        context.Services.AddSingleton<ISubscribedBlockHandler, SubscribedBlockHandler>();
        context.Services.AddTransient<IBlockChainDataHandler, LogEventDataHandler>();
        context.Services.AddTransient(typeof(IAeFinderClientEntityRepository<,>),
            typeof(AeFinderClientEntityRepository<,>));
        context.Services.AddSingleton(typeof(IBlockStateSetProvider<>), typeof(BlockStateSetProvider<>));
        context.Services.AddSingleton<IDAppDataProvider, DAppDataProvider>();
        context.Services.AddSingleton(typeof(IDAppDataIndexProvider<>), typeof(DAppDataIndexProvider<>));
        context.Services.AddSingleton<IAElfClientProvider, AElfClientProvider>();
        
        context.Services.Configure<ClientOptions>(o =>
        {
            o.DAppDataCacheCount = 5;
        });

        context.Services.Configure<NodeOptions>(o =>
        {
            o.NodeConfigList = new List<NodeConfig>
            {
                new NodeConfig { ChainId = "AELF", Endpoint = "http://mainchain.io" }
            };
        });
    }
    
    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var provider = context.ServiceProvider.GetRequiredService<IAeFinderClientInfoProvider>();
        provider.SetClientId(ClientId);
        provider.SetVersion(Version);
        AsyncHelper.RunSync(async () =>
            await CreateIndexAsync(context.ServiceProvider)
        );
    }

    private async Task CreateIndexAsync(IServiceProvider serviceProvider)
    {
        var types = GetTypesAssignableFrom<IIndexBuild>(typeof(AeFinderClientTestModule).Assembly);
        var elasticIndexService = serviceProvider.GetRequiredService<IElasticIndexService>();
        foreach (var t in types)
        {
            var indexName = $"{ClientId}-{Version}.{t.Name}".ToLower();
            await elasticIndexService.CreateIndexAsync(indexName, t);
        }
    }

    private async Task DeleteIndexAsync(IServiceProvider serviceProvider)
    {
        var elasticIndexService = serviceProvider.GetRequiredService<IElasticIndexService>();
        var types = GetTypesAssignableFrom<IIndexBuild>(typeof(AeFinderClientTestModule).Assembly);

        foreach (var t in types)
        {
            var indexName = $"{ClientId}-{Version}.{t.Name}".ToLower();
            await elasticIndexService.DeleteIndexAsync(indexName);
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
        AsyncHelper.RunSync(async () =>
            await DeleteIndexAsync(context.ServiceProvider)
        );
    }
}