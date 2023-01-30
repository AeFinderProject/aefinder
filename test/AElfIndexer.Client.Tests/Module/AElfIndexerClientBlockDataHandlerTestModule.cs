using AElfIndexer.Client.Handlers;
using AElfIndexer.Handler;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfIndexer.Module;

[DependsOn(typeof(AElfIndexerClientTestModule))]
public class AElfIndexerClientBlockDataHandlerTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IBlockChainDataHandler, MockBlockHandler>();
    }
}