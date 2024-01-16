using System.Reflection;
using AeFinder.BlockScan;
using AeFinder.Client.GraphQL;
using AeFinder.Client.Handlers;
using AeFinder.Client.Providers;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Services;
using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUglify.Helpers;
using Orleans;
using Orleans.Streams;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AeFinder.Client;

public abstract class AeFinderClientPluginBaseModule<TModule, TSchema, TQuery> : AbpModule 
    where TModule : AeFinderClientPluginBaseModule<TModule,TSchema,TQuery>
    where TSchema : AeFinderClientSchema<TQuery>
{
    protected abstract string ClientId { get; }
    protected abstract string Version { get; }
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TModule>(); });
        context.Services.AddSingleton<IAeFinderClientInfoProvider, AeFinderClientInfoProvider>();
        context.Services.AddSingleton<ISubscribedBlockHandler, SubscribedBlockHandler>();
        context.Services.AddTransient<IBlockChainDataHandler, LogEventDataHandler>();
        context.Services.AddTransient(typeof(IAeFinderClientEntityRepository<,>),
            typeof(AeFinderClientEntityRepository<,>));
        context.Services.AddSingleton(typeof(IBlockStateSetProvider<>), typeof(BlockStateSetProvider<>));
        context.Services.AddSingleton<IDAppDataProvider, DAppDataProvider>();
        context.Services.AddSingleton(typeof(IDAppDataIndexProvider<>), typeof(DAppDataIndexProvider<>));

        ConfigureServices(context.Services);
        ConfigNodes(context.Services);
        ConfigGraphQL(context.Services);
        
        Configure<DappMessageQueueOptions>(context.Services.GetConfiguration().GetSection("DappMessageQueue"));
        Configure<AeFinderClientOptions>(context.Services.GetConfiguration().GetSection("AeFinderClient"));
    }

    protected virtual void ConfigureServices(IServiceCollection serviceCollection)
    {
        
    }

    public override async Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        var provider = context.ServiceProvider.GetRequiredService<IAeFinderClientInfoProvider>();
        provider.SetClientId(ClientId);
        provider.SetVersion(Version);
        await CreateIndexAsync(context.ServiceProvider);
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        //TODO check clientId in db or ClientGrain
        app.UseGraphQL<TSchema>($"/{ClientId}/{typeof(TSchema).Name}/graphql");
        app.UseGraphQLPlayground(
            $"/{ClientId}/{typeof(TSchema).Name}/ui/playground",
            new PlaygroundOptions
            {
                GraphQLEndPoint = "../graphql",
                SubscriptionsEndPoint = "../graphql",
            });
        
        var clientOptions = new AeFinderClientOptions();
        context.GetConfiguration().GetSection("AeFinderClient").Bind(clientOptions);
        if (clientOptions.ClientType == AeFinderClientType.Full)
        {
            AsyncHelper.RunSync(async () => await InitBlockScanAsync(context));
        }
    }

    private async Task InitBlockScanAsync(ApplicationInitializationContext context)
    {
        var blockScanService = context.ServiceProvider.GetRequiredService<IBlockScanAppService>();
        var clusterClient = context.ServiceProvider.GetRequiredService<IClusterClient>();
        // TODO: Is correct?
        var subscribedBlockHandler = context.ServiceProvider.GetRequiredService<ISubscribedBlockHandler>();
        var messageStreamIds = await blockScanService.GetMessageStreamIdsAsync(ClientId, Version);
        foreach (var streamId in messageStreamIds)
        {
            var stream =
                clusterClient
                    .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                    .GetStream<SubscribedBlockDto>(streamId, AeFinderApplicationConsts.MessageStreamNamespace);

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

        await blockScanService.StartScanAsync(ClientId, Version);
    }

    private void ConfigNodes(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IAElfClientService, AElfClientService>();
        serviceCollection.AddSingleton<IAElfClientProvider, AElfClientProvider>();
        Configure<NodeOptions>(serviceCollection.GetConfiguration().GetSection("Node"));
    }

    private void ConfigGraphQL(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<TSchema>();
    }
    
    private async Task CreateIndexAsync(IServiceProvider serviceProvider)
    {
        var types = GetTypesAssignableFrom<IIndexBuild>(typeof(TModule).Assembly);
        var elasticIndexService = serviceProvider.GetRequiredService<IElasticIndexService>();
        foreach (var t in types)
        {
            var indexName = $"{ClientId}-{Version}.{t.Name}".ToLower();
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