using System;
using System.Threading.Tasks;
using AeFinder.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Volo.Abp.Modularity.PlugIns;

namespace AeFinder.Dapp;

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
            Log.Information("Starting AeFinder.Dapp.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.AddAppSettingsSecretsJson()
                .UseAutofac()
                .UseSerilog();
            var clientOptions = new AeFinderClientOptions();
            configuration.GetSection("AeFinderClient").Bind(clientOptions);
            if (clientOptions.ClientType == AeFinderClientType.Full)
            {
                await builder.AddApplicationAsync<AeFinderDappModule>(options =>
                {
                    options.PlugInSources.AddFolder(builder.Configuration.GetSection("PlugIns")["Path"]);
                });
            }
            else if (clientOptions.ClientType == AeFinderClientType.Query)
            {
                await builder.AddApplicationAsync<AeFinderDappQueryModule>(options =>
                {
                    options.PlugInSources.AddFolder(builder.Configuration.GetSection("PlugIns")["Path"]);
                });
            }
            var app = builder.Build();
            await app.InitializeApplicationAsync();
            await app.RunAsync();
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
}