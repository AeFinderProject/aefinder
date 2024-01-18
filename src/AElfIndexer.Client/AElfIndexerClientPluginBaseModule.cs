using System.Reflection;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Services;
using AElfIndexer.BlockScan;
using AElfIndexer.Client.GraphQL;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUglify.Helpers;
using Orleans;
using Orleans.Streams;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElfIndexer.Client;

public abstract class AElfIndexerClientPluginBaseModule<TModule, TSchema, TQuery> : AbpModule 
    where TModule : AElfIndexerClientPluginBaseModule<TModule,TSchema,TQuery>
    where TSchema : AElfIndexerClientSchema<TQuery>
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TModule>(); });
        context.Services.AddSingleton<ISubscribedBlockHandler, SubscribedBlockHandler>();
        context.Services.AddSingleton<IAppStateProvider, AppStateProvider>();
        context.Services.AddSingleton(typeof(IAppDataIndexProvider<>), typeof(AppDataIndexProvider<>));

        ConfigureServices(context.Services);
        ConfigNodes(context.Services);
        ConfigGraphQL(context.Services);
        
        Configure<AppInfoOptions>(context.Services.GetConfiguration().GetSection("AppInfo"));
    }

    protected virtual void ConfigureServices(IServiceCollection serviceCollection)
    {
        
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {        
        var appInfoOptions = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<AppInfoOptions>>().Value;
        AsyncHelper.RunSync(async () => await CreateIndexAsync(context.ServiceProvider, appInfoOptions.ScanAppId, appInfoOptions.Version));

        var app = context.GetApplicationBuilder();
        //TODO check clientId in db or ClientGrain
        app.UseGraphQL<TSchema>($"/{appInfoOptions.ScanAppId}/{typeof(TSchema).Name}/graphql");
        app.UseGraphQLPlayground(
            $"/{appInfoOptions.ScanAppId}/{typeof(TSchema).Name}/ui/playground",
            new PlaygroundOptions
            {
                GraphQLEndPoint = "../graphql",
                SubscriptionsEndPoint = "../graphql",
            });
        
        var clientOptions = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<ClientOptions>>().Value;
        if (clientOptions.ClientType == ClientType.Full)
        {
            AsyncHelper.RunSync(async () => await InitBlockScanAsync(context, appInfoOptions.ScanAppId, appInfoOptions.Version));
        }
    }

    private async Task InitBlockScanAsync(ApplicationInitializationContext context, string scanAppId, string version)
    {
        var blockScanService = context.ServiceProvider.GetRequiredService<IBlockScanAppService>();
        var clusterClient = context.ServiceProvider.GetRequiredService<IClusterClient>();
        // TODO: Is correct?
        var subscribedBlockHandler = context.ServiceProvider.GetRequiredService<ISubscribedBlockHandler>();
        var messageStreamIds = await blockScanService.GetMessageStreamIdsAsync(scanAppId, version);
        foreach (var streamId in messageStreamIds)
        {
            var stream =
                clusterClient
                    .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                    .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

            var subscriptionHandles = await stream.GetAllSubscriptionHandles();
            if (!subscriptionHandles.IsNullOrEmpty())
            {
                subscriptionHandles.ForEach(async x =>
                    await x.ResumeAsync(subscribedBlockHandler.HandleAsync));
            }
            else
            {
                await stream.SubscribeAsync(subscribedBlockHandler.HandleAsync);
            }
        }

        await blockScanService.StartScanAsync(scanAppId, version);
    }

    private void ConfigNodes(IServiceCollection serviceCollection)
    {
        Configure<ChainNodeOptions>(serviceCollection.GetConfiguration().GetSection("ChainNode"));
    }

    private void ConfigGraphQL(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<TSchema>();
    }
    
    private async Task CreateIndexAsync(IServiceProvider serviceProvider,string scanAppId, string version)
    {
        var types = GetTypesAssignableFrom<IIndexBuild>(typeof(TModule).Assembly);
        var elasticIndexService = serviceProvider.GetRequiredService<IElasticIndexService>();
        foreach (var t in types)
        {
            var indexName = $"{scanAppId}-{version}.{t.Name}".ToLower();
            //TODO Need to confirm shard and numberOfReplicas
            await elasticIndexService.CreateIndexAsync(indexName, t, 5,1);
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
}