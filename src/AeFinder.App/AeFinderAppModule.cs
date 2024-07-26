using System.Reflection;
using System.Runtime.Loader;
using AeFinder.App.BlockChain;
using AeFinder.App.BlockState;
using AeFinder.App.Handlers;
using AeFinder.App.Metrics;
using AeFinder.App.OperationLimits;
using AeFinder.App.Repositories;
using AeFinder.BlockScan;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Subscriptions;
using AeFinder.Options;
using AElf.EntityMapping.Elasticsearch;
using AeFinder.Sdk;
using AeFinder.Sdk.Entities;
using AElf.EntityMapping.Elasticsearch.Options;
using AElf.EntityMapping.Elasticsearch.Services;
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
        Configure<AppIndexOptions>(configuration.GetSection("AppIndex"));

        context.Services.AddSingleton(typeof(IAppDataIndexProvider<>), typeof(AppDataIndexProvider<>));
        context.Services.AddTransient(typeof(IRepository<>), typeof(Repository<>));
        context.Services.AddTransient(typeof(IReadOnlyRepository<>), typeof(ReadOnlyRepository<>));
        context.Services.AddSingleton<Instrumentation>();
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
        var blockStateInitializationProvider = context.ServiceProvider.GetRequiredService<IAppBlockStateInitializationProvider>();
        if (appInfoOptions.ClientType == ClientType.Full)
        {
            AsyncHelper.RunSync(async () => await CreateIndexAsync(context.ServiceProvider, appInfoOptions.AppId, appInfoOptions.Version));
            AsyncHelper.RunSync(blockStateInitializationProvider.InitializeAsync);
            AsyncHelper.RunSync(async () => await InitBlockPushAsync(context, appInfoOptions.AppId, appInfoOptions.Version));
        }
    }

    private async Task CreateIndexAsync(IServiceProvider serviceProvider, string appId, string version)
    {
        var esOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<ElasticsearchOptions>>().Value;
        var clusterClient = serviceProvider.GetRequiredService<IClusterClient>();
        var code = await clusterClient
            .GetGrain<IAppSubscriptionGrain>(GrainIdHelper.GenerateAppSubscriptionGrainId(appId)).GetCodeAsync(version);
        var assembly = AssemblyLoadContext.Default.LoadFromStream(new MemoryStream(code));
        var types = GetTypesAssignableFrom<IAeFinderEntity>(assembly);
        var elasticIndexService = serviceProvider.GetRequiredService<IElasticIndexService>();
        var appIndexOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<AppIndexOptions>>().Value;
        foreach (var t in types)
        {
            var indexName = $"{appId}-{version}.{t.Name}".ToLower();
            await elasticIndexService.CreateIndexAsync(indexName, t, esOptions.NumberOfShards,
                esOptions.NumberOfReplicas, appIndexOptions.IndexSettings);
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

    private async Task InitBlockPushAsync(ApplicationInitializationContext context, string appId, string version)
    {
        var blockScanService = context.ServiceProvider.GetRequiredService<IBlockScanAppService>();
        var clusterClient = context.ServiceProvider.GetRequiredService<IClusterClient>();
        var subscribedBlockHandler = context.ServiceProvider.GetRequiredService<ISubscribedBlockHandler>();
        var messageStreamIds = await blockScanService.GetMessageStreamIdsAsync(appId, version);
        foreach (var streamId in messageStreamIds)
        {
            await SubscribeStreamAsync(appId, streamId, subscribedBlockHandler, clusterClient);
        }

        await blockScanService.StartScanAsync(appId, version);
    }

    private async Task SubscribeStreamAsync(string appId, Guid streamId,
        ISubscribedBlockHandler subscribedBlockHandler, IClusterClient clusterClient)
    {
        var streamNamespaceGrain =
            clusterClient.GetGrain<IMessageStreamNamespaceManagerGrain>(GrainIdHelper
                .GenerateMessageStreamNamespaceManagerGrainId());
        var streamNamespace = await streamNamespaceGrain.GetMessageStreamNamespaceAsync(appId);

        var stream =
            clusterClient
                .GetStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .GetStream<SubscribedBlockDto>(streamNamespace, streamId);

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
}