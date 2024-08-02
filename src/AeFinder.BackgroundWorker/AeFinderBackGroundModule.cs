using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.BackgroundWorker.ScheduledTask;
using AeFinder.Kubernetes;
using AeFinder.Kubernetes.Manager;
using AeFinder.MongoDb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AeFinderKubernetesModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderApplicationModule),
    typeof(AbpBackgroundWorkersModule))]
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
        ConfigureTokenCleanupService();
        ConfigureCache(configuration);
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
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        AsyncHelper.RunSync(() => context.AddBackgroundWorkerAsync<GrainDataClearWorker>());
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {

    }
}