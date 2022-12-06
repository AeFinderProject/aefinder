using AElfIndexer.EntityFrameworkCore;
using AElfIndexer.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfIndexer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AElfIndexerApplicationModule),
    typeof(AElfIndexerEntityFrameworkCoreModule),
    typeof(AElfIndexerGrainsModule))]
public class AElfIndexerOrleansSiloModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<AElfIndexerHostedService>();
    }
}