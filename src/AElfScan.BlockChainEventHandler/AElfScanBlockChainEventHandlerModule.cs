using AElfScan.Options;
using AElfScan.Orleans;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfScanOrleansEventSourcingModule),
    typeof(AElfScanBlockChainEventHandlerCoreModule),
    typeof(AbpEventBusRabbitMqModule))]
public class AElfScanBlockChainEventHandlerModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        // context.Services.AddHostedService<AElfScanClusterClientHostedService>();
        
        Configure<OrleansClientOption>(configuration.GetSection("OrleansClient"));
        context.Services.AddSingleton<AElfScanClusterClientHostedService>();
        context.Services.AddSingleton<IHostedService>(sp => sp.GetService<AElfScanClusterClientHostedService>());
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<AElfScanClusterClientHostedService>().OrleansClient);
        context.Services.AddSingleton<IGrainFactory>(sp =>
            sp.GetService<AElfScanClusterClientHostedService>().OrleansClient);
    }
}
