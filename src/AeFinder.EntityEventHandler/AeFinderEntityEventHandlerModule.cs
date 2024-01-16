using AeFinder.EntityEventHandler.Core;
using AeFinder.Grains;
using AeFinder.Grains.Grain.BlockScan;
using AElf.Indexing.Elasticsearch.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace AeFinder.EntityEventHandler;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AeFinderEntityEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule))]
public class AeFinderEntityEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureTokenCleanupService();
        ConfigureEsIndexCreation();
        context.Services.AddHostedService<AeFinderHostedService>();
        
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
                    parts.AddApplicationPart(typeof(AeFinderGrainsModule).Assembly).WithReferences())
                //.AddSimpleMessageStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async ()=> await client.Connect());
        
        AsyncHelper.RunSync(async () =>
        {
            var grain = client.GetGrain<IBlockScanCheckGrain>(AeFinderApplicationConsts.BlockScanCheckGrainId);
            await grain.Start();
        });
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }

    //Create the ElasticSearch Index based on Domain Entity
    private void ConfigureEsIndexCreation()
    {
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(AeFinderDomainModule)); });
    }
    
    //Disable TokenCleanupService
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
}