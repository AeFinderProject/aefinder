using AElfIndexer.Grains;
using AElfIndexer.Providers;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfIndexer;

[DependsOn(typeof(AbpAutoMapperModule),
    typeof(AElfIndexerGrainsModule))]
public class AElfIndexerBlockChainEventHandlerCoreModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AElfIndexerBlockChainEventHandlerCoreModule>();
        });
    }
}