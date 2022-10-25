using Volo.Abp.Modularity;
using Volo.Abp.AutoMapper;

namespace AElfScan;

[DependsOn(typeof(AbpAutoMapperModule),
    typeof(AElfScanApplicationModule),
    typeof(AElfScanApplicationContractsModule))]
public class AElfScanEntityEventHandlerCoreModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AElfScanEntityEventHandlerCoreModule>();
        });
    }
}