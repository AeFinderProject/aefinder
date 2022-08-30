using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AElfScanOrleansEventSourcingModule))]
public class AElfScanOrleansSiloModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<AElfScanHostedService>();
    }
}