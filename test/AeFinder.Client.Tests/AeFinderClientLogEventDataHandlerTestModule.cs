using AeFinder.Client.Handlers;
using AeFinder.Grains.State.Client;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.Client;

[DependsOn(typeof(AeFinderClientTestModule))]
public class AeFinderClientLogEventDataHandlerTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IAElfLogEventProcessor<LogEventInfo>, MockTokenTransferredProcessor>();
    }
}