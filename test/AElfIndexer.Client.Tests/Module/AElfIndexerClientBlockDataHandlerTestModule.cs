using AElfIndexer.Client.Handlers;
using AElfIndexer.Handler;
using Microsoft.Extensions.DependencyInjection;

namespace AElfIndexer.Module;

public class AElfIndexerClientBlockDataHandlerTestModule : AElfIndexerClientTestModule
{
    protected override void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IBlockChainDataHandler, MockBlockHandler>();
    }
}