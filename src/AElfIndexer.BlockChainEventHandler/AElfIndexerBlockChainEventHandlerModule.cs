using AElfIndexer.DTOs;
using AElfIndexer.Grains;
using AElfIndexer.Providers;
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

namespace AElfIndexer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfIndexerGrainsModule),
    typeof(AElfIndexerBlockChainEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule))]
public class AElfIndexerBlockChainEventHandlerModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        // context.Services.AddHostedService<AElfIndexerClusterClientHostedService>();
        Configure<BlockChainEventHandlerOptions>(configuration.GetSection("BlockChainEventHandler"));
        
        context.Services.AddSingleton<AElfIndexerClusterClientHostedService>();
        context.Services.AddSingleton<IHostedService>(sp => sp.GetService<AElfIndexerClusterClientHostedService>());
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<AElfIndexerClusterClientHostedService>().OrleansClient);
        context.Services.AddSingleton<IGrainFactory>(sp =>
            sp.GetService<AElfIndexerClusterClientHostedService>().OrleansClient);
    }
    
}
