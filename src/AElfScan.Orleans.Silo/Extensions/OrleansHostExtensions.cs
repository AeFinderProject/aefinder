using System;
using EventStore.ClientAPI;
using JsonNet.PrivateSettersContractResolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.EventSourcing.Snapshot;
using Orleans.EventSourcing.Snapshot.Hosting;
using Orleans.Hosting;
using Orleans.Providers.MongoDB.Configuration;

namespace AElfScan.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleansSnapshot<TGrain>(this IHostBuilder hostBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        var configSection = configuration.GetSection("OrleansServer");
        if (configSection == null)
            throw new ArgumentNullException(nameof(configSection), "The OrleansServer node is missing");
        return hostBuilder.UseOrleans(siloBuilder =>
        {
            //Configure OrleansSnapshot
            siloBuilder.UseLocalhostClustering()
                // .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                // .AddMongoDBGrainStorage("Default",(MongoDBGrainStorageOptions op) =>
                // {
                //     op.CollectionPrefix = "GrainStorage";
                //     op.DatabaseName = configSection.GetValue<string>("DataBase");
                //
                //     op.ConfigureJsonSerializerSettings = jsonSettings =>
                //     {
                //         jsonSettings.ContractResolver = new PrivateSetterContractResolver();
                //     };
                // })
                .AddRedisGrainStorageAsDefault(optionsBuilder => optionsBuilder.Configure(options =>
                {
                    options.DataConnectionString = configSection.GetValue<string>("KVrocksConnection"); // This is the deafult
                    options.UseJson = true;
                    options.DatabaseNumber = 1;
                }))
                .AddSnapshotStorageBasedConsistencyProviderAsDefault((op, name) =>
                {
                    op.UseIndependentEventStorage = true;
                    // Should configure event storage when set UseIndependentEventStorage true
                    op.ConfigureIndependentEventStorage = (services, name) =>
                    {
                        var eventStoreConnectionString = configSection.GetValue<string>("EventStoreConnection");
                        var eventStoreConnection = EventStoreConnection.Create(eventStoreConnectionString);
                        eventStoreConnection.ConnectAsync().Wait();
                
                        services.AddSingleton(eventStoreConnection);
                        services.AddSingleton<IGrainEventStorage, EventStoreGrainEventStorage>();
                    };
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(TGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => { logging.SetMinimumLevel(LogLevel.Debug).AddConsole(); });
        });
    }
}