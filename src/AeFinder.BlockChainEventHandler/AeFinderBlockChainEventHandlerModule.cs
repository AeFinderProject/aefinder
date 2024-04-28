using System;
using AeFinder.Grains;
using AeFinder.MongoDb;
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
    typeof(AbpCachingStackExchangeRedisModule))]
public class AeFinderBlockChainEventHandlerModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockChainEventHandlerOptions>(configuration.GetSection("BlockChainEventHandler"));
        
        context.Services.AddHostedService<AeFinderClientHostedService>();
        ConfigureCache(configuration);

        // context.Services.AddSingleton<AeFinderClientHostedService>();
        // context.Services.AddSingleton<IHostedService>(sp => sp.GetService<AeFinderClientHostedService>());
        // context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<AeFinderClientHostedService>().OrleansClient);
        // context.Services.AddSingleton<IGrainFactory>(sp =>
        //     sp.GetService<AeFinderClientHostedService>().OrleansClient);
        
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
                .Configure<ClientMessagingOptions>(options =>
                {
                    options.ResponseTimeout = TimeSpan.FromSeconds(Convert.ToInt32(configuration["Orleans:GrainResponseTimeOut"]));
                    options.MaxMessageBodySize = Convert.ToInt32(configuration["Orleans:GrainMaxMessageBodySize"]);
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(AeFinderGrainsModule).Assembly).WithReferences())
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
    }
    
    private void ConfigureCache(IConfiguration configuration)
    {
        Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "AeFinder:"; });
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
