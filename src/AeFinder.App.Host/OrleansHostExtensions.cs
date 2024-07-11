using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;
using Orleans.Streams.Kafka.Config;

namespace AeFinder.App;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansClient(this IHostBuilder hostBuilder)
    {
        return hostBuilder.UseOrleansClient((context, clientBuilder) =>
        {
            var configuration = context.Configuration;
            var configSection = context.Configuration.GetSection("Orleans");
            if (configSection == null)
                throw new ArgumentNullException(nameof(configSection), "The Orleans config node is missing");
            clientBuilder.UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configSection.GetValue<string>("DataBase");
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                })
                .AddMemoryStreams(AeFinderApplicationConsts.MessageStreamName)
                .AddKafka(AeFinderApplicationConsts.MessageStreamName)
                .WithOptions(options =>
                {
                    options.BrokerList = configuration.GetSection("Kafka:Brokers").Get<List<string>>();
                    options.ConsumerGroupId = "AeFinder";
                    options.ConsumeMode = ConsumeMode.LastCommittedMessage;
                    
                    var partitions = configuration.GetSection("Kafka:Partitions").Get<int>();
                    var replicationFactor = configuration.GetSection("Kafka:ReplicationFactor").Get<short>();

                    foreach (var n in configuration.GetSection("BlockPush:MessageStreamNamespaces").Get<List<string>>())
                    {
                        options.AddTopic(n, new TopicCreationConfig
                        {
                            AutoCreate = true,
                            Partitions = partitions,
                            ReplicationFactor = replicationFactor
                        });
                    }

                    foreach (var n in configuration.GetSection("BlockPush:HistoricalMessageStreamNamespaces")
                                 .Get<List<string>>())
                    {
                        options.AddTopic(n, new TopicCreationConfig
                        {
                            AutoCreate = true,
                            Partitions = partitions,
                            ReplicationFactor = replicationFactor
                        });
                    }
                })
                .AddJson();
        });
    }
}