using AElfIndexer.Grains;
using AElfIndexer.MongoDB;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfIndexer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AElfIndexerMongoDbModule),
    typeof(AElfIndexerApplicationModule),
    typeof(AElfIndexerGrainsModule))]
public class AElfIndexerOrleansSiloModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<AElfIndexerHostedService>();
    }
}