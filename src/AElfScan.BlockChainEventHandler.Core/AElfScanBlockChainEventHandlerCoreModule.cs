using AElfScan.Grains;
using AElfScan.Providers;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(typeof(AbpAutoMapperModule),
    typeof(AElfScanGrainsModule))]
public class AElfScanBlockChainEventHandlerCoreModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AElfScanBlockChainEventHandlerCoreModule>();
        });
    }
}