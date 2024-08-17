using System;
using AeFinder.Grains;
using AeFinder.MongoDb;
using AElf.OpenTelemetry;
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
using Volo.Abp.Threading;

namespace AeFinder.BlockChainEventHandler;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AeFinderGrainsModule),
    typeof(AeFinderBlockChainEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderApplicationModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(OpenTelemetryModule))]
public class AeFinderBlockChainEventHandlerModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockChainEventHandlerOptions>(configuration.GetSection("BlockChainEventHandler"));
        
        context.Services.AddHostedService<AeFinderClientHostedService>();
        ConfigureCache(configuration);
    }
    
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AeFinder:"; });
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {

    }

    
}
