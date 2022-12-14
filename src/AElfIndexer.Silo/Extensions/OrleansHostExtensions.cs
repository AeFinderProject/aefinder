using System;
using System.Net;
using AElfIndexer.Grains.Grain.Blocks;
using JsonNet.PrivateSettersContractResolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Configuration.Overrides;
using Orleans.Hosting;
using Orleans.Providers;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Statistics;
using Serilog;

namespace AElfIndexer.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansSnapshot<TGrain>(this IHostBuilder hostBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        var configSection = configuration.GetSection("Orleans");
        if (configSection == null)
            throw new ArgumentNullException(nameof(configSection), "The OrleansServer node is missing");
        return hostBuilder.UseOrleans(siloBuilder =>
        {
            //Configure OrleansSnapshot
            siloBuilder
                // .UseRedisClustering(opt =>
                // {
                //     opt.ConnectionString = configSection.GetValue<string>("ClusterDbConnection");
                //     opt.Database = configSection.GetValue<int>("ClusterDbNumber");
                // })
                .ConfigureEndpoints(advertisedIP:IPAddress.Parse(configSection.GetValue<string>("AdvertisedIP")),siloPort: configSection.GetValue<int>("SiloPort"), gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
                // .UseLocalhostClustering()
                .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                // .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings = settings =>
                // {
                //     settings.NullValueHandling = NullValueHandling.Include;
                //     settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                //     settings.DefaultValueHandling = DefaultValueHandling.Populate;
                // })
                .AddMongoDBGrainStorage("Default",(MongoDBGrainStorageOptions op) =>
                {
                    op.CollectionPrefix = "GrainStorage";
                    op.DatabaseName = configSection.GetValue<string>("DataBase");
                
                    op.ConfigureJsonSerializerSettings = jsonSettings =>
                    {
                        // jsonSettings.ContractResolver = new PrivateSetterContractResolver();
                        jsonSettings.NullValueHandling = NullValueHandling.Include;
                        jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                        jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    };
                    
                })
                .Configure<GrainCollectionOptions>(options =>
                {
                    // Set the value of CollectionAge to 10 minutes for all grain
                    // options.CollectionAge = TimeSpan.FromMinutes(10);

                    // Override the value of CollectionAge to 1 minutes for BlockGrain
                    options.ClassSpecificCollectionAge[typeof(BlockGrain).FullName] =
                        TimeSpan.FromSeconds(61);
                })
                // .AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                // {
                //     options.DataConnectionString = configSection.GetValue<string>("GrainStorageDbConnection"); 
                //     options.DatabaseNumber = configSection.GetValue<int>("GrainStorageDbNumber");
                //     options.UseJson = true;
                // }))
                // .UseRedisReminderService(options =>
                // {
                //     options.ConnectionString = configSection.GetValue<string>("ClusterDbConnection");
                //     options.DatabaseNumber = configSection.GetValue<int>("ClusterDbNumber");
                // })
                .UseMongoDBReminders(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.CreateShardKeyForCosmos = false;
                })
                // .AddSnapshotStorageBasedConsistencyProviderAsDefault((op, name) =>
                // {
                //     op.UseIndependentEventStorage = true;
                //     // Should configure event storage when set UseIndependentEventStorage true
                //     op.ConfigureIndependentEventStorage = (services, name) =>
                //     {
                //         var eventStoreConnectionString = configSection.GetValue<string>("EventStoreConnection");
                //         var eventStoreConnection = EventStoreConnection.Create(eventStoreConnectionString);
                //         eventStoreConnection.ConnectAsync().Wait();
                //     
                //         services.AddSingleton(eventStoreConnection);
                //         services.AddSingleton<IGrainEventStorage, EventStoreGrainEventStorage>();
                //     };
                // })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                })
                .AddSimpleMessageStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .AddMemoryGrainStorage("PubSubStore")
                .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                .UseDashboard(options => {
                    options.Username = configSection.GetValue<string>("DashboardUserName");
                    options.Password = configSection.GetValue<string>("DashboardPassword");
                    options.Host = "*";
                    options.Port = configSection.GetValue<int>("DashboardPort");
                    options.HostSelf = true;
                    options.CounterUpdateIntervalMs = configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                })
                .UseLinuxEnvironmentStatistics()
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
        });
    }
}