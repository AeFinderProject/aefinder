using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using AElfIndexer.Handler;
using Microsoft.Extensions.DependencyInjection;

namespace AElfIndexer.Module;

public class AElfIndexerClientLogEventDataHandlerTestModule : AElfIndexerClientTestModule
{
    protected override void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IAElfLogEventProcessor<LogEventInfo>, MockTokenTransferredProcessor>();
    }
}