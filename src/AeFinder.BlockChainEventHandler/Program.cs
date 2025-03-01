﻿using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler.ABP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.OpenTelemetry;

namespace AeFinder.BlockChainEventHandler
{
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
                .WriteTo.OpenTelemetry(
                    endpoint: "http://localhost:4316/v1/logs",
                    protocol: OtlpProtocol.HttpProtobuf)
#endif
                .CreateLogger();

            try
            {
                Log.Information("Starting AeFinder.BlockChainEventHandler.");
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
                .ConfigureAppConfiguration(build =>
                {
                    build.AddJsonFile("appsettings.secrets.json", optional: true);
                })
                .InitAppConfiguration(true)
                .UseApolloForHostBuilder()
                .UseOrleansClient()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddApplication<AeFinderBlockChainEventHandlerModule>();
                })
                .UseAutofac()
                .UseAElfExceptionHandler()
                .UseSerilog();
    }
}