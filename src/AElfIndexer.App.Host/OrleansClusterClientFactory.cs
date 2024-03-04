using System.Collections.Generic;
using AElfIndexer.Grains;
using Microsoft.Extensions.Configuration;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;

namespace AElfIndexer.App.Host;

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
                parts.AddApplicationPart(typeof(AElfIndexerGrainsModule).Assembly).WithReferences())
            //.ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
            .AddKafka(AElfIndexerApplicationConsts.MessageStreamName)
            .WithOptions(options =>
            {
                options.BrokerList = configuration.GetSection("Kafka:Brokers").Get<List<string>>();
                options.ConsumerGroupId = "AElfIndexer";
                options.ConsumeMode = ConsumeMode.LastCommittedMessage;
                options.AddTopic(AElfIndexerApplicationConsts.MessageStreamNamespace,new TopicCreationConfig { AutoCreate = true });
                options.MessageMaxBytes = configuration.GetSection("Kafka:MessageMaxBytes").Get<int>();
            })
            .AddJson()
            .Build()
            .Build();
    }
}