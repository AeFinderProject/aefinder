using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using AElfIndexer.Handler;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfIndexer.Module;

[DependsOn(typeof(AElfIndexerClientTestModule))]
public class AElfIndexerClientLogEventDataHandlerTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IAElfLogEventProcessor<LogEventInfo>, MockTokenTransferredProcessor>();
    }
}