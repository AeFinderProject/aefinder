using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace AeFinder.BlockChainEventHandler;

public class AeFinderClientHostedService:IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;
    // private readonly ILogger<AeFinderClientHostedService> _logger;
    // public IClusterClient OrleansClient { get;}

    public AeFinderClientHostedService(
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
        //         parts.AddApplicationPart(typeof(AeFinderGrainsModule).Assembly).WithReferences())
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