using AElf.Indexing.Elasticsearch.Options;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElfIndexer.Client;

public class AElfIndexerClientPluginBaseModule<TModule, TSchema, TQuery> : AbpModule 
    where TModule : AElfIndexerClientPluginBaseModule<TModule,TSchema,TQuery>
    where TSchema : AElfIndexerClientSchema<TQuery>
    where TQuery : AElfIndexerClientQuery
{
    protected virtual string ClientId { get; }
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureEsIndexCreation();
        ConfigureGraphType(context.Services);
        context.Services.AddSingleton<TQuery>();
        context.Services.AddSingleton<TSchema>();
        context.Services.AddSingleton<ISchema, TSchema>();
        context.Services.AddTransient(typeof(IAElfIndexerClientEntityRepository<,,,>),
            typeof(AElfIndexerClientEntityRepository<,,,>));
        //context.Services.AddControllers().PartManager.ApplicationParts.Add(new AssemblyPart(typeof(TModule).Assembly));
    }

    protected virtual void ConfigureGraphType(IServiceCollection serviceCollection)
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