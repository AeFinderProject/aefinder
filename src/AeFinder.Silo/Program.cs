using AeFinder.Grains.Grain.Blocks;
using AeFinder.Silo.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace AeFinder.Silo;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = LogHelper.CreateLogger(LogEventLevel.Debug);
        
        try
        {
            Log.Information("Starting AeFinder.Silo.");
            await CreateHostBuilder(args).RunConsoleAsync();
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
            .ConfigureServices((hostcontext, services) => { services.AddApplication<AeFinderOrleansSiloModule>(); })

            // .UseOrleans<BlockGrain>()
            .UseOrleansSnapshot<BlockGrain>()
            .UseAutofac()
            .UseSerilog();
}