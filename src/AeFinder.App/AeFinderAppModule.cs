using AeFinder.App.BlockChain;
using AeFinder.App.BlockState;
using AeFinder.App.Handlers;
using AeFinder.App.OperationLimits;
using AeFinder.App.Repositories;
using AeFinder.BlockScan;
using AElf.EntityMapping.Elasticsearch;
using AeFinder.Sdk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUglify.Helpers;
using Orleans;
using Orleans.Streams;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;
using Volo.Abp.Serialization;
using Volo.Abp.Threading;

namespace AeFinder.App;

[DependsOn(typeof(AbpSerializationModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpAutofacModule),
    typeof(AElfEntityMappingElasticsearchModule))]
public class AeFinderAppModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderAppModule>(); });
        
        var configuration = context.Services.GetConfiguration();
        Configure<MessageQueueOptions>(configuration.GetSection("MessageQueue"));
        Configure<ChainNodeOptions>(configuration.GetSection("ChainNode"));
        Configure<AppInfoOptions>(configuration.GetSection("AppInfo"));
        Configure<AppStateOptions>(configuration.GetSection("AppState"));
        Configure<OperationLimitOptions>(configuration.GetSection("OperationLimit"));

        context.Services.AddSingleton(typeof(IAppDataIndexProvider<>), typeof(AppDataIndexProvider<>));
        context.Services.AddTransient(typeof(IEntityRepository<>), typeof(EntityRepository<>));
        context.Services.AddTransient(typeof(IReadOnlyRepository<>), typeof(ReadOnlyRepository<>));
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
        
        var appInfoOptions = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<AppInfoOptions>>().Value;
        var appInfoProvider = context.ServiceProvider.GetRequiredService<IAppInfoProvider>();
        appInfoProvider.SetAppId(appInfoOptions.AppId);
        appInfoProvider.SetVersion(appInfoOptions.Version);
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {        
        var appInfoOptions = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<AppInfoOptions>>().Value;
        if (appInfoOptions.ClientType == ClientType.Full)
        {
            AsyncHelper.RunSync(async () => await InitBlockScanAsync(context, appInfoOptions.AppId, appInfoOptions.Version));
        }
    }
    
    private async Task InitBlockScanAsync(ApplicationInitializationContext context, string appId, string version)
    {
        var blockScanService = context.ServiceProvider.GetRequiredService<IBlockScanAppService>();
        var clusterClient = context.ServiceProvider.GetRequiredService<IClusterClient>();
        var subscribedBlockHandler = context.ServiceProvider.GetRequiredService<ISubscribedBlockHandler>();
        var messageStreamIds = await blockScanService.GetMessageStreamIdsAsync(appId, version);
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

        await blockScanService.StartScanAsync(appId, version);
    }
}