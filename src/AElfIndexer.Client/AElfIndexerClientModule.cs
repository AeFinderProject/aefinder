using AElfIndexer.Grains.Grain.Client;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElfIndexer.Client;

public class AElfIndexerClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AElfIndexerClientModule>(); });
        
        var configuration = context.Services.GetConfiguration();
        Configure<ClientOptions>(configuration.GetSection("Client"));
    }
}