using AElfScan;
using AElfScan.Silo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;
using Serilog.Events;
using Orleans;

public class Program
{
    public static async Task<int> Main(string[] args)
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
            .CreateLogger();

        try
        {
            Log.Information("Starting AElfScan.Silo.");
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

    internal static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) => { services.AddApplication<AElfScanSiloModule>(); })
            .UseOrleans((context, builder) =>
            {
                builder
                    //.ConfigureDefaults()
                    .UseLocalhostClustering()
                    .AddAdoNetGrainStorageAsDefault(options =>
                    {
                        options.Invariant = "MySql.Data.MySqlConnector";
                        options.ConnectionString = context.Configuration["ConnectionStrings:Default"];
                        //options.UseJsonFormat = true;
                    })
                    .ConfigureApplicationParts(parts =>
                        parts.AddApplicationPart(typeof(AElfScanApplicationModule).Assembly).WithReferences());
            })
            .UseAutofac()
            .UseSerilog();
}