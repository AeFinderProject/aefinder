using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;
using AElfScan.Extensions;
using AElfScan.Grains.Grain;

namespace AElfScan;

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
            .WriteTo.Async(c => c.Console())
            .CreateLogger();
        
        try
        {
            Log.Information("Starting AElfScan.Silo.");

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
            .ConfigureServices((hostcontext, services) =>
            {
                services.AddApplication<AElfScanOrleansSiloModule>();
            })
            // .UseOrleans<BlockGrain>()
            .UseOrleansSnapshot<BlockGrain>()
            .UseAutofac()
            .UseSerilog();
    
}