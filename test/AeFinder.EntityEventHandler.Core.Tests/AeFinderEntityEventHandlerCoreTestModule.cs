using AeFinder.Orleans.TestBase;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AeFinder.EntityEventHandler.Core.Tests;

[DependsOn(typeof(AeFinderEntityEventHandlerCoreModule),
    typeof(AeFinderOrleansTestBaseModule),
    typeof(AeFinderDomainTestModule))]
public class AeFinderEntityEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AeFinderEntityEventHandlerCoreTestModule>();
        });
        
    }
}