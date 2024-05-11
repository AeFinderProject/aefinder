using System.Collections.Generic;
using AeFinder.Grains;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;

namespace AeFinder.App;

public class OrleansClusterClientFactory
{
    public static IClusterClient GetClusterClient(IConfiguration configuration)
    {
        return new ClientBuilder()
            .ConfigureDefaults()
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
            //.ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
            .AddKafka(AeFinderApplicationConsts.MessageStreamName)
            .WithOptions(options =>
            {
                options.BrokerList = configuration.GetSection("Kafka:Brokers").Get<List<string>>();
                options.ConsumerGroupId = "AeFinder";
                options.ConsumeMode = ConsumeMode.LastCommittedMessage;
                options.AddTopic(AeFinderApplicationConsts.MessageStreamNamespace,new TopicCreationConfig
                {
                    AutoCreate = true, 
                    Partitions = configuration.GetSection("Kafka:Partitions").Get<int>(),
                    ReplicationFactor = configuration.GetSection("Kafka:ReplicationFactor").Get<short>()
                });
                options.MessageMaxBytes = configuration.GetSection("Kafka:MessageMaxBytes").Get<int>();
            })
            .AddJson()
            .Build()
            .Build();
    }
}