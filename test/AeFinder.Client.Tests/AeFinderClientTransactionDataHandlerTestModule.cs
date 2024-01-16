using AeFinder.Client.Handlers;
using AeFinder.Grains.State.Client;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.Client;

[DependsOn(typeof(AeFinderClientTestModule))]
public class AeFinderClientTransactionDataHandlerTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IBlockChainDataHandler, MockTransactionHandler>();
        context.Services.AddTransient<IAElfLogEventProcessor<TransactionInfo>, MockTokenTransferredTransactionProcessor>();
    }
}