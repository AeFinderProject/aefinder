using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace AeFinder.BackgroundWorker
{
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
                .CreateLogger();

            try
            {
                Log.Information("Starting AeFinder.BackGround.");
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
                .ConfigureAppConfiguration(build => { build.AddJsonFile("appsettings.secrets.json", optional: true); })
                .InitAppConfiguration(true)
                .UseApolloForHostBuilder()
                .ConfigureServices((hostContext, services) => { services.AddApplication<AeFinderBackGroundModule>(); })
                .UseOrleansClient()
                .UseAutofac()
                .UseSerilog();
    }
}