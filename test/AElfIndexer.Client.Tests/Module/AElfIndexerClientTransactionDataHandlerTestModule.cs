using AElfIndexer.Client.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfIndexer.Client.Module;

[DependsOn(typeof(AElfIndexerClientLogEventDataHandlerTestModule))]
public class AElfIndexerClientTransactionDataHandlerTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IBlockChainDataHandler, MockTransactionHandler>();
    }
}