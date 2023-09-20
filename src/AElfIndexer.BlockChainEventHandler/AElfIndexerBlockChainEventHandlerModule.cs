using AElfIndexer.DTOs;
using AElfIndexer.Grains;
using AElfIndexer.MongoDB;
using AElfIndexer.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElfIndexer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfIndexerGrainsModule),
    typeof(AElfIndexerBlockChainEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AElfIndexerMongoDbModule),
    typeof(AElfIndexerApplicationModule),
    typeof(AbpCachingStackExchangeRedisModule))]
public class AElfIndexerBlockChainEventHandlerModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockChainEventHandlerOptions>(configuration.GetSection("BlockChainEventHandler"));
        
        context.Services.AddHostedService<AElfIndexerClusterClientHostedService>();
        ConfigureCache(configuration);

        // context.Services.AddSingleton<AElfIndexerClusterClientHostedService>();
        // context.Services.AddSingleton<IHostedService>(sp => sp.GetService<AElfIndexerClusterClientHostedService>());
        // context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<AElfIndexerClusterClientHostedService>().OrleansClient);
        // context.Services.AddSingleton<IGrainFactory>(sp =>
        //     sp.GetService<AElfIndexerClusterClientHostedService>().OrleansClient);
        
        context.Services.AddSingleton<IClusterClient>(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                // .UseRedisClustering(opt =>
                // {
                //     opt.ConnectionString = configuration["Orleans:ClusterDbConnection"];
                //     opt.Database = Convert.ToInt32(configuration["Orleans:ClusterDbNumber"]);
                // })
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(AElfIndexerGrainsModule).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
    }
    
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AElfIndexer:"; });
    }
    
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async ()=> await client.Connect());
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }

    
}
