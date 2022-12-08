using AElf.Indexing.Elasticsearch.Options;
using AElfIndexer.BlockScan;
using AElfIndexer.Grains.Grain.BlockScan;
using GraphQL.Server.Ui.Playground;
using GraphQL.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using NUglify.Helpers;
using Orleans;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

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

        AsyncHelper.RunSync(async () => await ResumeStreamAsync(context));
    }

    private async Task ResumeStreamAsync(ApplicationInitializationContext context)
    {
        var clusterClient = context.ServiceProvider.GetRequiredService<IClusterClient>();
        var clientGrain = clusterClient.GetGrain<IClientGrain>(ClientId);
        var streamId = await clientGrain.GetMessageStreamIdAsync();
        var stream =
            clusterClient
                .GetStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamId, AElfIndexerApplicationConsts.MessageStreamNamespace);

        var subscriptionHandles = await stream.GetAllSubscriptionHandles();
        if (!subscriptionHandles.IsNullOrEmpty())
        {
            // subscriptionHandles.ForEach(
            //     async x => await x.ResumeAsync(_subscribedBlockHandler.HandleAsync));
        }
        else
        {
            //await stream.SubscribeAsync(_subscribedBlockHandler.HandleAsync);
        }
    }
}