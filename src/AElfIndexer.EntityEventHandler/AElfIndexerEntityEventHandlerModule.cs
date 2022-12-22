using AElf.Indexing.Elasticsearch.Options;
using AElfIndexer;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.BlockScan;
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
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace AElfIndexer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AElfIndexerEntityEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule))]
public class AElfIndexerEntityEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        ConfigureTokenCleanupService();
        ConfigureEsIndexCreation();
        context.Services.AddHostedService<AElfIndexerHostedService>();
        
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
                .AddSimpleMessageStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
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
            var grain = client.GetGrain<IBlockScanCheckGrain>(AElfIndexerApplicationConsts.BlockScanCheckGrainId);
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
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(AElfIndexerDomainModule)); });
    }
    
    //Disable TokenCleanupService
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
}