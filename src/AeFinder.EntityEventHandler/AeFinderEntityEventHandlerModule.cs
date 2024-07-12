using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockPush;
using AElf.EntityMapping.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace AeFinder.EntityEventHandler;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AeFinderEntityEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AbpCachingStackExchangeRedisModule))]
public class AeFinderEntityEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureTokenCleanupService();
        ConfigureEsIndexCreation();
        context.Services.AddHostedService<AeFinderHostedService>();
        ConfigureCache(configuration);
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        // AsyncHelper.RunSync(async ()=> await client.Connect());
        
        AsyncHelper.RunSync(async () =>
        {
            var grain = client.GetGrain<IBlockPushCheckGrain>(GrainIdHelper.GenerateBlockPushCheckGrainId());
            await grain.Start();
        });
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {

    }

    //Create the ElasticSearch Index & Initialize field cache based on Domain Entity
    private void ConfigureEsIndexCreation()
    {
        Configure<CollectionCreateOptions>(x => { x.AddModule(typeof(AeFinderDomainModule)); });
    }
    
    //Disable TokenCleanupService
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AeFinder:"; });
    }
    
}