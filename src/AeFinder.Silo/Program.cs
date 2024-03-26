using AeFinder.Grains.Grain.Blocks;
using AeFinder.Silo.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;

namespace AeFinder.Silo;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(configuration)
#if DEBUG
            .WriteTo.Async(c => c.Console())
#endif
            // .WriteTo.Async(c => c.File("Logs/logs.txt"))
            .CreateLogger();
        
        try
        {
            Log.Information("Starting AeFinder.Silo.");

            // await CreateHostBuilder(args).RunConsoleAsync();
            var host = CreateHostBuilder(args).Build();

            // Listen to the application stopping event
            var lifetime = host.Services.GetService<IHostApplicationLifetime>();
            var siloHost = host.Services.GetRequiredService<ISiloHost>();

            lifetime.ApplicationStopping.Register(async () =>
            {
                await siloHost.StopAsync();
                // Optionally, you can also log that the application is stopping.
                Log.Information("Gracefully stop Silo.");
            });

            // Run the host
            await host.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    internal static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostcontext, services) =>
            {
                services.AddApplication<AeFinderOrleansSiloModule>();
            })
            // .UseOrleans<BlockGrain>()
            .UseOrleansSnapshot<BlockGrain>()
            .UseAutofac()
            .UseSerilog();
    
}