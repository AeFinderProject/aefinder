using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(typeof(AElfScanOrleansEventSourcingModule),
    typeof(AElfScanOrleansEventSourcingModule))]
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