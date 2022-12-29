using AElfIndexer.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;

namespace AElfIndexer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(AElfIndexerApplicationModule),
    typeof(AElfIndexerGrainsModule))]
public class AElfIndexerOrleansSiloModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<AElfIndexerHostedService>();
        ConfigureTokenCleanupService();
    }
    
    //Disable TokenCleanupService
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }
}