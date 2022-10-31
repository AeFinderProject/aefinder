using AElf.Indexing.Elasticsearch.Options;
using AElfScan;
using AElfScan.Orleans;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElfScan;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AElfScanEntityEventHandlerCoreModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AbpEventBusRabbitMqModule))]
public class AElfScanEntityEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureEsIndexCreation();
        context.Services.AddHostedService<AElfScanHostedService>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var clientService = context.ServiceProvider.GetRequiredService<IClusterClientAppService>();
        AsyncHelper.RunSync(clientService.StartAsync);
        
        AsyncHelper.RunSync(async () =>
        {
            var grain = clientService.Client.GetGrain<IBlockScanCheckGrain>(AElfScanApplicationConsts.BlockScanCheckGrainId);
            await grain.Start();
        });
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var clientService = context.ServiceProvider.GetService<IClusterClientAppService>();
        AsyncHelper.RunSync(clientService.StopAsync);
    }

    //Create the ElasticSearch Index based on Domain Entity
    private void ConfigureEsIndexCreation()
    {
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(AElfScanDomainModule)); });
    }
}