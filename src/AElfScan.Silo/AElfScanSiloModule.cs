using AElfScan.EntityFrameworkCore;
using AElfScan.Orleans;
using AElfScan.ScanClients;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElfScan.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AElfScanApplicationModule),
    typeof(AElfScanEntityFrameworkCoreModule),
    typeof(AbpAspNetCoreSerilogModule))]
public class AElfScanSiloModule : AbpModule
{
    public ISiloHost SiloHost;

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<AElfScanHostedService>();
        var configuration = context.Services.GetConfiguration();

        // var builder = new SiloHostBuilder()
        //     .UseLocalhostClustering()
        //     .AddAdoNetGrainStorageAsDefault(options =>
        //     {
        //         options.Invariant = "MySql.Data.MySqlConnector";
        //         options.ConnectionString = configuration["ConnectionStrings:Default"];
        //         //options.UseJsonFormat = true;
        //     })
        //     //.ConfigureServices((hostContext, services) => { services.AddApplication<AElfScanSiloModule>(); })
        //     .ConfigureServices(service =>
        //     {
        //         service.AddApplication<AElfScanSiloModule>();
        //         //service.AddTransient<IBlockProvider, BlockProvider>();
        //     })
        //     .ConfigureApplicationParts(parts =>
        //         parts.AddApplicationPart(typeof(AElfScanApplicationModule).Assembly).WithReferences());
        //
        // SiloHost = builder.Build();
        // AsyncHelper.RunSync(async () => await SiloHost.StartAsync());
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        //AsyncHelper.RunSync(async () => await SiloHost.StopAsync());
    }
}