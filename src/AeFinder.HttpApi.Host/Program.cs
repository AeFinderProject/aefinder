using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.ExceptionHandler.ABP;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace AeFinder;

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
            .WriteTo.OpenTelemetry(
                endpoint: "http://localhost:4316/v1/logs",
                protocol: OtlpProtocol.HttpProtobuf)
#endif
            .CreateLogger();
        
        try
        {
            var workerThreads = configuration.GetValue<int>("ThreadPool:MinWorkerThreads", 0);
            var completionPortThreads = configuration.GetValue<int>("ThreadPool:MinCompletionPortThreads", 0);
            if (workerThreads > 0 && completionPortThreads > 0)
            {
                if (!ThreadPool.SetMinThreads(workerThreads, completionPortThreads))
                {
                    throw new Exception(
                        $"Set min threads failed! MinWorkerThreads: {workerThreads}, MinCompletionPortThreads:{completionPortThreads}");
                }
            }
            
            Log.Information("Starting AeFinder.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.AddAppSettingsSecretsJson()
                .InitAppConfiguration(false)
                .UseApolloForConfigureHostBuilder()
                .UseOrleansClient()
                .UseAutofac()
                .UseAElfExceptionHandler()
                .UseSerilog();
            await builder.AddApplicationAsync<AeFinderHttpApiHostModule>();
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