using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Orleans;

public class ClusterClientAppService : AElfScanAppService, IClusterClientAppService, ISingletonDependency
{
    public IClusterClient Client { get; }

    public ILogger<ClusterClientAppService> Logger { get; set; }

    public ClusterClientAppService(ILoggerProvider loggerProvider)
    {
        Client = new ClientBuilder()
            .ConfigureDefaults()
            .UseLocalhostClustering()
            .ConfigureLogging(builder => builder.AddProvider(loggerProvider))
            .Build();
    }

    public async Task StartAsync()
    {
        try
        {
            var attempt = 0;
            var maxAttempts = 20;
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
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        

        ;
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