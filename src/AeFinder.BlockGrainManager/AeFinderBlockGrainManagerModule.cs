using AeFinder.Grains;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AeFinder.BlockGrainManager;

public class AeFinderBlockGrainManagerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockGrainManagerOptions>(configuration.GetSection("BlockGrainManager"));

        context.Services.AddHostedService<AeFinderClusterClientHostedService>();
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AeFinderBlockGrainManagerModule>();
        });
        ConfigureCache(configuration);

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
                    options.DatabaseName = configuration["Orleans:DataBase"];
                    ;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
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
        AsyncHelper.RunSync(async () => await client.Connect());
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }
}