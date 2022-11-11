using AElfScan.DTOs;
using AElfScan.Grains;
using AElfScan.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfScanGrainsModule),
    typeof(AElfScanBlockChainEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule))]
public class AElfScanBlockChainEventHandlerModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        // context.Services.AddHostedService<AElfScanClusterClientHostedService>();
        
        context.Services.AddSingleton<AElfScanClusterClientHostedService>();
        context.Services.AddSingleton<IHostedService>(sp => sp.GetService<AElfScanClusterClientHostedService>());
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<AElfScanClusterClientHostedService>().OrleansClient);
        context.Services.AddSingleton<IGrainFactory>(sp =>
            sp.GetService<AElfScanClusterClientHostedService>().OrleansClient);
    }
    
}
