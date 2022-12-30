using AElfIndexer.MongoDB;
using Volo.Abp.Modularity;
using Volo.Abp.AutoMapper;

namespace AElfIndexer;

[DependsOn(typeof(AbpAutoMapperModule),
    typeof(AElfIndexerMongoDbModule),
    typeof(AElfIndexerApplicationModule),
    typeof(AElfIndexerApplicationContractsModule))]
public class AElfIndexerEntityEventHandlerCoreModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            //Add all mappings defined in the assembly of the MyModule class
            options.AddMaps<AElfIndexerEntityEventHandlerCoreModule>();
        });
    }
}