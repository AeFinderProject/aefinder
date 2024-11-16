using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.Core;
using AeFinder.BackgroundWorker.Options;
using AeFinder.BackgroundWorker.ScheduledTask;
using AeFinder.Kubernetes;
using AeFinder.Kubernetes.Manager;
using AeFinder.Metrics;
using AeFinder.MongoDb;
using AElf.EntityMapping.Options;
using AElf.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace AeFinder.BackgroundWorker;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AeFinderBackGroundCoreModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AeFinderKubernetesModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderApplicationModule),
    typeof(AbpBackgroundWorkersModule),
    typeof(OpenTelemetryModule))]
public class AeFinderBackGroundModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddHttpClient();
        context.Services.AddHostedService<AeFinderHostedService>();
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderBackGroundModule>(); });
        context.Services.AddTransient<IAppDeployManager, KubernetesAppManager>();
        context.Services.AddTransient<IAppResourceLimitProvider, AppResourceLimitProvider>();
        context.Services.AddTransient<IKubernetesAppMonitor, KubernetesAppMonitor>();
        ConfigureTokenCleanupService();
        ConfigureEsIndexCreation();
        ConfigureCache(configuration);
        ConfigureMongoDbService(configuration, context);
        context.Services.Configure<ScheduledTaskOptions>(configuration.GetSection("ScheduledTask"));
        context.Services.Configure<TransactionRepairOptions>(configuration.GetSection("TransactionRepair"));
    }
    
    //Disable TokenCleanupBackgroundWorker
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
    
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AeFinder:"; });
    }

    private void ConfigureMongoDbService(IConfiguration configuration,ServiceConfigurationContext context)
    {
        // Register MongoDB Settings
        context.Services.Configure<OrleansDataClearOptions>(configuration.GetSection("OrleansDataClear"));
        // Register MongoClient
        context.Services.AddSingleton<IMongoClient>(serviceProvider =>
        {
            var mongoDbSettings = serviceProvider.GetRequiredService<IOptions<OrleansDataClearOptions>>().Value;
            return new MongoClient(mongoDbSettings.MongoDBClient);
        });
        context.Services.AddSingleton<IOrleansDbClearService, OrleansDbClearService>();
    }
    
    private void ConfigureEsIndexCreation()
    {
        Configure<CollectionCreateOptions>(x => { x.AddModule(typeof(AeFinderDomainModule)); });
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppDataClearWorker>());
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppRescanCheckWorker>());
        // AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppInfoSyncWorker>());
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppPodListSyncWorker>());
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<AppPodResourceInfoSyncWorker>());

        var transactionRepairOptions = context.ServiceProvider
            .GetRequiredService<IOptionsSnapshot<TransactionRepairOptions>>().Value;
        if (transactionRepairOptions.Enable)
        {
            AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<TransactionRepairWorker>());
        }
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {

    }
}