using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Blocks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace AElfIndexer;

public class AElfIndexerClusterClientHostedService:IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;
    // private readonly ILogger<AElfIndexerClusterClientHostedService> _logger;
    // public IClusterClient OrleansClient { get;}

    public AElfIndexerClusterClientHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider)
    {
        _application = application;
        _serviceProvider = serviceProvider;
        // _logger = logger;
        // OrleansClient = new ClientBuilder()
        //     // .UseRedisClustering(opt =>
        //     // {
        //     //     opt.ConnectionString = configuration["Orleans:ClusterDbConnection"];
        //     //     opt.Database = Convert.ToInt32(configuration["Orleans:ClusterDbNumber"]);
        //     // })
        //     .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
        //     .UseMongoDBClustering(options =>
        //     {
        //         options.DatabaseName = configuration["Orleans:DataBase"];;
        //         options.Strategy = MongoDBMembershipStrategy.SingleDocument;
        //     })
        //     .ConfigureApplicationParts(parts =>
        //         parts.AddApplicationPart(typeof(AElfIndexerGrainsModule).Assembly).WithReferences())
        //     // .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IBlockGrain).Assembly).WithReferences())
        //     .Configure<ClusterOptions>(options =>
        //     {
        //         options.ClusterId = configuration["Orleans:ClusterId"];
        //         options.ServiceId = configuration["Orleans:ServiceId"];
        //     })
        //     // .Configure<ClientMessagingOptions>(options =>
        //     // {
        //     //     options.ResponseTimeout = TimeSpan.MaxValue;
        //     // })
        //     .Build();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _application.Initialize(_serviceProvider);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}