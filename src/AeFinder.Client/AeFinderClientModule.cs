using AeFinder.Grains.Grain.Client;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AeFinder.Client;

public class AeFinderClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderClientModule>(); });
        
        var configuration = context.Services.GetConfiguration();
        Configure<ClientOptions>(configuration.GetSection("Client"));
    }
}