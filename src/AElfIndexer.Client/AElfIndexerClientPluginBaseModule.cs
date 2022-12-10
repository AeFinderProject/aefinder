using System.Reflection;
using AElf.Indexing.Elasticsearch;
using AElf.Indexing.Elasticsearch.Services;
using AElfIndexer.Client.GraphQL;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElfIndexer.Client;

public abstract class AElfIndexerClientPluginBaseModule<TModule, TSchema, TQuery, T> : AbpModule 
    where TModule : AElfIndexerClientPluginBaseModule<TModule,TSchema,TQuery, T>
    where TSchema : AElfIndexerClientSchema<TQuery>
{
    protected abstract string ClientId { get; }
    protected abstract string Version { get; }
    
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IAElfIndexerClientInfoProvider<T>, AElfIndexerClientInfoProvider<T>>();
        context.Services.AddSingleton<ISubscribedBlockHandler<T>, SubscribedBlockHandler<T>>();
        context.Services.AddTransient<IBlockChainDataHandler<T>, LogEventDataHandler<T>>();
        context.Services.AddTransient(typeof(IAElfIndexerClientEntityRepository<,,,>),
            typeof(AElfIndexerClientEntityRepository<,,,>));
        
        ConfigureServices(context.Services);
        ConfigNodes(context.Services);
        ConfigGraphQL(context.Services);
    }

    protected virtual void ConfigureServices(IServiceCollection serviceCollection)
    {
        
    }

    public override async Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        var provider = context.ServiceProvider.GetRequiredService<IAElfIndexerClientInfoProvider<T>>();
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
            var indexName = $"{ClientId}_{Version}.{t.Name}".ToLower();
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