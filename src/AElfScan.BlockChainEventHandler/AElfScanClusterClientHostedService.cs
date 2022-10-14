using System.Net;
using AElfScan.Grain;
using AElfScan.Options;
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
    private readonly OrleansClientOption _orleansClientOption;
    public IClusterClient OrleansClient { get;}

    public AElfScanClusterClientHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        ILogger<AElfScanClusterClientHostedService> logger,
        IOptionsSnapshot<OrleansClientOption> orleansClientOption,
        IServiceProvider serviceProvider)
    {
        _application = application;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _orleansClientOption = orleansClientOption.Value;
        List<IPEndPoint> nodes = new List<IPEndPoint>();
        foreach (var node in _orleansClientOption.NodeIpAddresses)
        {
            nodes.Add(new IPEndPoint(IPAddress.Parse(node), _orleansClientOption.GatewayPort));
        }
        OrleansClient = new ClientBuilder()
            .UseStaticClustering(nodes.ToArray())
            // .UseLocalhostClustering()
            .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IBlockGrain).Assembly).WithReferences())
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = _orleansClientOption.ClusterId;
                options.ServiceId = _orleansClientOption.ServiceId;
            })
            .Build();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _application.Initialize(_serviceProvider);

        _logger.LogInformation("before connect:"+OrleansClient.IsInitialized);
        await OrleansClient.Connect();
        _logger.LogInformation("after connect:"+OrleansClient.IsInitialized);
        _logger.LogInformation("Client successfully connected to silo host \n");
        
        return;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await OrleansClient.Close();
        OrleansClient.Dispose();
        
        _application.Shutdown();
        return;
    }
}