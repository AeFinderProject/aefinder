using AElf.EntityMapping.Options;
using AElfIndexer.Grains;
using AElfIndexer.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;

namespace AElfIndexer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AElfIndexerMongoDbModule),
    typeof(AElfIndexerApplicationModule),
    typeof(AElfIndexerGrainsModule),
    typeof(AbpCachingStackExchangeRedisModule))]
public class AElfIndexerOrleansSiloModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddHostedService<AElfIndexerHostedService>();
        ConfigureTokenCleanupService();
        ConfigureCache(configuration);
        // ConfigureEsIndexCreation();
    }
    
    //Disable TokenCleanupService
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AElfIndexer:"; });
    }
    //Create the ElasticSearch Index & Initialize field cache based on Domain Entity
    // private void ConfigureEsIndexCreation()
    // {
    //     Configure<CollectionCreateOptions>(x => { x.AddModule(typeof(AElfIndexerDomainModule)); });
    // }
}