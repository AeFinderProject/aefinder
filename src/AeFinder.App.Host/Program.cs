using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler.ABP;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace AeFinder.App;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        
        var appId = configuration["AppInfo:AppId"];
        var version = configuration["AppInfo:Version"];
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("AppId", appId)
            .Enrich.WithProperty("Version", version)
            .ReadFrom.Configuration(configuration)
#if DEBUG
            .WriteTo.Async(c => c.Console())
            .WriteTo.OpenTelemetry(
                endpoint: "http://localhost:4316/v1/logs",
                protocol: OtlpProtocol.HttpProtobuf)
#endif
            .CreateLogger();

        try
        {
            Log.Information("Starting AeFinder.App.Host.");
            await CreateHostBuilder(args).Build().RunAsync();
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
    
    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder(args)
            .UseOrleansClient()
            .UseAutofac()
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
            .UseAElfExceptionHandler()
            .UseSerilog();
    }
}