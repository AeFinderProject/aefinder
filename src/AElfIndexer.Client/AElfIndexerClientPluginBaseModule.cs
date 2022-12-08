using AElf.Indexing.Elasticsearch.Options;
using GraphQL.Server.Ui.Playground;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElfIndexer.Client;

public abstract class AElfIndexerClientPluginBaseModule<TModule, TSchema, TQuery> : AbpModule 
    where TModule : AElfIndexerClientPluginBaseModule<TModule,TSchema,TQuery>
    where TSchema : AElfIndexerClientSchema<TQuery>
{
    protected abstract string ClientId { get; }
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureEsIndexCreation();
        ConfigureServices(context.Services);
        context.Services.AddSingleton<TSchema>();
        context.Services.AddTransient(typeof(IAElfIndexerClientEntityRepository<,,,>),
            typeof(AElfIndexerClientEntityRepository<,,,>));
    }

    protected virtual void ConfigureServices(IServiceCollection serviceCollection)
    {
        
    }
        
    private void ConfigureEsIndexCreation()
    {
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(TModule)); });
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
}