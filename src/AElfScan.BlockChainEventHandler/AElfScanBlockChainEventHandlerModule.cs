using AElfScan.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfScanBlockChainEventHandlerCoreModule),
    typeof(AElfScanApplicationModule))]
public class AElfScanBlockChainEventHandlerModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        context.Services.AddHostedService<AElfScanHostedService>();
    }
}