using AeFinder.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AeFinder.BlockChainEventHandler;

[DependsOn(typeof(AbpAutoMapperModule),
    typeof(AeFinderGrainsModule))]
public class AeFinderBlockChainEventHandlerCoreModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AeFinderBlockChainEventHandlerCoreModule>();
        });
    }
}