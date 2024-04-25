using AeFinder.MongoDb;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AeFinder.EntityEventHandler;

[DependsOn(typeof(AbpAutoMapperModule),
    typeof(AeFinderMongoDbModule),
    typeof(AeFinderApplicationModule),
    typeof(AeFinderApplicationContractsModule))]
public class AeFinderEntityEventHandlerCoreModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AeFinderEntityEventHandlerCoreModule>();
        });
    }
}