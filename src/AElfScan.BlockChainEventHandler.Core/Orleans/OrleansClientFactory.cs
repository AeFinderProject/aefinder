using System.Net;
using System.Threading.Tasks;
using AElfScan.Grain;
using AElfScan.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;

namespace AElfScan.Orleans;

public class OrleansClientFactory:IOrleansClusterClientFactory
{
    private readonly OrleansClientOption _orleansClientOption;
    private readonly ILogger<OrleansClientFactory> _logger;

    public OrleansClientFactory(
        IOptionsSnapshot<OrleansClientOption> orleansClientOption,
        ILogger<OrleansClientFactory> logger)
    {
        _orleansClientOption = orleansClientOption.Value;
        _logger = logger;
    }

    public async Task<IClusterClient> GetClient()
    {
        var endPoints = new List<IPEndPoint>();

        var ipAddress = IPAddress.Loopback;
        var port = 30000;

        endPoints.Add(new IPEndPoint(ipAddress, port));
        
        IClusterClient client = new ClientBuilder()
            .UseLocalhostClustering()
            .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IBlockGrain).Assembly).WithReferences())
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = _orleansClientOption.ClusterId;
                options.ServiceId = _orleansClientOption.ServiceId;
            })
            // .Configure<ClusterOptions>(options =>
            // {
            //     options.ClusterId = "dev";
            //     options.ServiceId = "dev";
            // })
            // .UseStaticClustering(endPoints.ToArray())
            // .ConfigureLogging(logging => logging.AddConsole())
            .Build();

        _logger.LogInformation("before connect:"+client.IsInitialized);
        await client.Connect();
        _logger.LogInformation("after connect:"+client.IsInitialized);
        _logger.LogInformation("Client successfully connected to silo host \n");
        return client;
    }
}