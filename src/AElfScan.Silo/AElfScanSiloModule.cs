using AElfScan.Grain;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfScan.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfScanGrainModule),
    typeof(AbpAspNetCoreSerilogModule))]
public class AElfScanSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<AElfScanHostedService>();
        var configuration = context.Services.GetConfiguration();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
    }
}