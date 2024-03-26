using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Statistics;

namespace AeFinder.Silo.Extensions;

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
                .ConfigureEndpoints(advertisedIP: IPAddress.Parse(configSection.GetValue<string>("AdvertisedIP")),
                    siloPort: configSection.GetValue<int>("SiloPort"),
                    gatewayPort: configSection.GetValue<int>("GatewayPort"), listenOnAnyHostAddress: true)
                // .UseLocalhostClustering()
                .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                // .Configure<JsonGrainStateSerializerOptions>(options => options.ConfigureJsonSerializerSettings = settings =>
                // {
                //     settings.NullValueHandling = NullValueHandling.Include;
                //     settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                //     settings.DefaultValueHandling = DefaultValueHandling.Populate;
                // })
                .AddMongoDBGrainStorage("Default", (MongoDBGrainStorageOptions op) =>
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
                    // Override the value of CollectionAge to
                    var collection = configSection.GetSection("ClassSpecificCollectionAge").GetChildren();
                    foreach (var item in collection)
                    {
                        options.ClassSpecificCollectionAge[item.Key] = TimeSpan.FromSeconds(int.Parse(item.Value));
                    }
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
                .Configure<SiloMessagingOptions>(options =>
                {
                    options.ResponseTimeout = TimeSpan.FromSeconds(configSection.GetValue<int>("GrainResponseTimeOut"));
                    options.MaxMessageBodySize = configSection.GetValue<int>("GrainMaxMessageBodySize");
                    options.MaxForwardCount = configSection.GetValue<int>("MaxForwardCount");
                })
                //.AddSimpleMessageStreamProvider(AeFinderApplicationConsts.MessageStreamName)
                .AddMongoDBGrainStorage("PubSubStore", options =>
                {
                    // Config PubSubStore Storage for Persistent Stream 
                    options.CollectionPrefix = "StreamStorage";
                    options.DatabaseName = configSection.GetValue<string>("DataBase");

                    options.ConfigureJsonSerializerSettings = jsonSettings =>
                    {
                        jsonSettings.NullValueHandling = NullValueHandling.Include;
                        jsonSettings.DefaultValueHandling = DefaultValueHandling.Populate;
                        jsonSettings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                    };
                })
                .ConfigureApplicationParts(parts => parts.AddFromApplicationBaseDirectory())
                .UseDashboard(options =>
                {
                    options.Username = configSection.GetValue<string>("DashboardUserName");
                    options.Password = configSection.GetValue<string>("DashboardPassword");
                    options.Host = "*";
                    options.Port = configSection.GetValue<int>("DashboardPort");
                    options.HostSelf = true;
                    options.CounterUpdateIntervalMs = configSection.GetValue<int>("DashboardCounterUpdateIntervalMs");
                })
                .UseLinuxEnvironmentStatistics()
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
                /*.AddKafka(AeFinderApplicationConsts.MessageStreamName)
                .WithOptions(options =>
                {
                    options.BrokerList = configuration.GetSection("Kafka:Brokers").Get<List<string>>();
                    options.ConsumerGroupId = "AeFinder";
                    options.ConsumeMode = ConsumeMode.LastCommittedMessage;
                    options.AddTopic(AeFinderApplicationConsts.MessageStreamNamespace,
                        new TopicCreationConfig { AutoCreate = true });
                    options.MessageMaxBytes = configuration.GetSection("Kafka:MessageMaxBytes").Get<int>();
                })
                .AddJson()*/
                //.AddLoggingTracker()
                //.Build();
        });
    }
}