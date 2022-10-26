using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Orleans;

public class ClusterClientAppService : IClusterClientAppService, ISingletonDependency
{
    public IClusterClient Client { get;}

    public ILogger<ClusterClientAppService> Logger { get; set; }

    public ClusterClientAppService(ILoggerProvider loggerProvider,IConfiguration configuration)
    {
        Client = new ClientBuilder()
            .ConfigureDefaults()
            .UseRedisClustering(opt =>
            {
                opt.ConnectionString = configuration["Redis:Configuration"];
            })
            .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(AElfScanOrleansEventSourcingModule).Assembly).WithReferences())
            .AddSimpleMessageStreamProvider(AElfScanApplicationConsts.MessageStreamName)
            .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
            .Build();
    }

    public async Task StartAsync()
    {
        var attempt = 0;
        var maxAttempts = 10;
        var delay = TimeSpan.FromSeconds(1);
        await Client.Connect(async error =>
        {
            if (++attempt < maxAttempts)
            {
                Logger.LogWarning(error,
                    "Failed to connect to Orleans cluster on attempt {@Attempt} of {@MaxAttempts}.",
                    attempt, maxAttempts);
                await Task.Delay(delay);
                return true;
            }
            else
            {
                Logger.LogError(error,
                    "Failed to connect to Orleans cluster on attempt {@Attempt} of {@MaxAttempts}.",
                    attempt, maxAttempts);

                return false;
            }
        });
    }

    public async Task StopAsync()
    {
        try
        {
            await Client.Close();
        }
        catch (OrleansException error)
        {
            Logger.LogWarning(error,
                "Error while gracefully disconnecting from Orleans cluster. Will ignore and continue to shutdown.");
        }
    }
}