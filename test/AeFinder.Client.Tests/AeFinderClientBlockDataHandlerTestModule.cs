using AeFinder.Client.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.Client;

[DependsOn(typeof(AeFinderClientTestModule))]
public class AeFinderClientBlockDataHandlerTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IBlockChainDataHandler, MockBlockHandler>();
    }
}