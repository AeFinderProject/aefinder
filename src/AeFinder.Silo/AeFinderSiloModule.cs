using AeFinder.App.Deploy;
using AeFinder.Apps;
using AeFinder.Common;
using AeFinder.Commons;
using AeFinder.Grains;
using AeFinder.MongoDb;
using AElf.ExceptionHandler.Orleans.Extensions;
using AElf.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers.MongoDB.StorageProviders.Serializers;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace AeFinder.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderApplicationModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AeFinderGrainsModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(OpenTelemetryModule))]
public class AeFinderOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddHostedService<AeFinderHostedService>();
        ConfigureTokenCleanupService();
        ConfigureCache(configuration);
        context.Services.AddTransient<IAppResourceLimitProvider, AppResourceLimitProvider>();
        context.Services.AddTransient<IElasticSearchIndexHelper, ElasticSearchIndexIndexHelper>();
        context.Services.AddOrleansExceptionHandler();
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
    //Create the ElasticSearch Index & Initialize field cache based on Domain Entity
    // private void ConfigureEsIndexCreation()
    // {
    //     Configure<CollectionCreateOptions>(x => { x.AddModule(typeof(AeFinderDomainModule)); });
    // }
}