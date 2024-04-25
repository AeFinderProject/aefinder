using AeFinder.Block;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.EntityEventHandler;

[DependsOn(typeof(AeFinderEntityEventHandlerCoreTestModule))]
public class EntityEventHandlerCoreBlockIndexTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IBlockAppService, MockBlockAppService>();
    }
}