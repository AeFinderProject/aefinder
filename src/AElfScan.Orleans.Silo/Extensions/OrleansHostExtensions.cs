using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace AElfScan.Extensions;

public static class OrleansHostExtensions
{
    public static IHostBuilder UseOrleans<TGrain>(this IHostBuilder hostBuilder)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        var configSection = configuration.GetSection("OrleansServer");
        if (configSection == null) throw new ArgumentNullException(nameof(configSection),"The OrleansServer node is missing");
        return hostBuilder.UseOrleans(siloBuilder =>
        {
            //Configure Orleans
            siloBuilder.UseLocalhostClustering()
                // .UseMongoDBClustering(options =>
                // {
                //     options.DatabaseName = "Test";
                //     options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                // })
                .AddMemoryGrainStorageAsDefault()
                .AddLogStorageBasedLogConsistencyProvider("LogStorage")
                .UseMongoDBClient(configSection.GetValue<string>("MongoDBClient"))
                .AddMongoDBGrainStorage("OrleansStorage",
                    options =>
                    {
                        options.DatabaseName = configSection.GetValue<string>("DataBase");
                        options.CreateShardKeyForCosmos = false;

                        options.ConfigureJsonSerializerSettings = settings =>
                        {
                            settings.NullValueHandling = NullValueHandling.Include;
                            settings.ObjectCreationHandling = ObjectCreationHandling.Replace;
                            settings.DefaultValueHandling = DefaultValueHandling.Populate;
                        };
                    })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configSection.GetValue<string>("ClusterId");
                    options.ServiceId = configSection.GetValue<string>("ServiceId");
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(TGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole());
        });
    }
}