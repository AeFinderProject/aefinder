using System.Net;
using AElfScan.Grains.Grain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;

namespace AElfScan;

public class AElfScanClusterClientHostedService:IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AElfScanClusterClientHostedService> _logger;
    public IClusterClient OrleansClient { get;}

    public AElfScanClusterClientHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        ILogger<AElfScanClusterClientHostedService> logger,
        IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _application = application;
        _serviceProvider = serviceProvider;
        _logger = logger;
        OrleansClient = new ClientBuilder()
            .UseRedisClustering(opt =>
            {
                opt.ConnectionString = configuration["Orleans:ClusterDbConnection"];
                opt.Database = Convert.ToInt32(configuration["Orleans:ClusterDbNumber"]);
            })
            .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IBlockGrain).Assembly).WithReferences())
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = configuration["Orleans:ClusterId"];
                options.ServiceId = configuration["Orleans:ServiceId"];
            })
            // .Configure<ClientMessagingOptions>(options =>
            // {
            //     options.ResponseTimeout = TimeSpan.MaxValue;
            // })
            .Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _application.Initialize(_serviceProvider);

        _logger.LogInformation("before connect OrleansClient.IsInitialized:"+OrleansClient.IsInitialized);
        await OrleansClient.Connect();
        _logger.LogInformation("after connect OrleansClient.IsInitialized:"+OrleansClient.IsInitialized);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await OrleansClient.Close();
        OrleansClient.Dispose();
        _logger.LogInformation("OrleansClient Closed");
        _application.Shutdown();
    }
}