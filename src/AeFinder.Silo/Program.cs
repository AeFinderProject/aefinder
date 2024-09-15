using AeFinder.Silo.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace AeFinder.Silo;

public class Program
{
    public async static Task<int> Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Is(LogEventLevel.Debug)
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Async(c =>
                c.Console(
                    outputTemplate:
                    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}"))
            .WriteTo.Async(c => c.File("Logs/log-.log", rollingInterval: RollingInterval.Day,
                outputTemplate:
                "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}{Offset:zzz}][{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}"))
#if DEBUG
            .WriteTo.OpenTelemetry(
                endpoint: "http://localhost:4316/v1/logs",
                protocol: OtlpProtocol.HttpProtobuf)
#endif
            .CreateLogger();

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
            .InitAppConfiguration(true)
            .UseApolloForHostBuilder()
            .ConfigureServices((hostcontext, services) => { services.AddApplication<AeFinderOrleansSiloModule>(); })
            .UseOrleans()
            .UseAutofac()
            .UseSerilog();
}