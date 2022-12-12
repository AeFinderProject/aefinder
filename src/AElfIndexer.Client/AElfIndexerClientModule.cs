using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfIndexer.Client;

public class AElfIndexerClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AElfIndexerClientModule>(); });
    }
}