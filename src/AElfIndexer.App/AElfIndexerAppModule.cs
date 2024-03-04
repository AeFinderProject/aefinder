using AElfIndexer.App.BlockChain;
using AElfIndexer.App.BlockState;
using AElfIndexer.App.Handlers;
using AElfIndexer.App.OperationLimits;
using AElfIndexer.BlockScan;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUglify.Helpers;
using Orleans;
using Orleans.Streams;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Serialization;
using Volo.Abp.Threading;

namespace AElfIndexer.App;

[DependsOn(typeof(AbpSerializationModule))]
public class AElfIndexerAppModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AElfIndexerAppModule>(); });
        
        var configuration = context.Services.GetConfiguration();
        Configure<MessageQueueOptions>(configuration.GetSection("MessageQueue"));
        Configure<ChainNodeOptions>(configuration.GetSection("ChainNode"));
        Configure<AppInfoOptions>(configuration.GetSection("AppInfo"));
        Configure<AppStateOptions>(configuration.GetSection("AppState"));
        Configure<OperationLimitOptions>(configuration.GetSection("OperationLimit"));

        context.Services.AddSingleton(typeof(IAppDataIndexProvider<>), typeof(AppDataIndexProvider<>));
    }
    
    public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
    {
        var operationLimitManager = context.ServiceProvider.GetRequiredService<IOperationLimitManager>();
        
        var entityOperationLimitProvider = context.ServiceProvider.GetRequiredService<IEntityOperationLimitProvider>();
        operationLimitManager.Add(entityOperationLimitProvider);
        
        var logOperationLimitProvider = context.ServiceProvider.GetRequiredService<ILogOperationLimitProvider>();
        operationLimitManager.Add(logOperationLimitProvider);
        
        var contractOperationLimitProvider = context.ServiceProvider.GetRequiredService<IContractOperationLimitProvider>();
        operationLimitManager.Add(contractOperationLimitProvider);
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {        
        var appInfoOptions = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<AppInfoOptions>>().Value;

        var app = context.GetApplicationBuilder();
        app.UseGraphQL($"/{appInfoOptions.AppId}/{appInfoOptions.Version}/graphql");
        // app.UseGraphQLPlayground(
        //     $"/{appInfoOptions.ScanAppId}/{typeof(TSchema).Name}/ui/playground",
        //     new PlaygroundOptions
        //     {
        //         GraphQLEndPoint = "../graphql",
        //         SubscriptionsEndPoint = "../graphql",
        //     });
        
        var clientOptions = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<AppInfoOptions>>().Value;
        if (clientOptions.ClientType == ClientType.Full)
        {
            AsyncHelper.RunSync(async () => await InitBlockScanAsync(context, appInfoOptions.AppId, appInfoOptions.Version));
        }
    }
    
    private async Task InitBlockScanAsync(ApplicationInitializationContext context, string scanAppId, string version)
    {
        var blockScanService = context.ServiceProvider.GetRequiredService<IBlockScanAppService>();
        var clusterClient = context.ServiceProvider.GetRequiredService<IClusterClient>();
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
}