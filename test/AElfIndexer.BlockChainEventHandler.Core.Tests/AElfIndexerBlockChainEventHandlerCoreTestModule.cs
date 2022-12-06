using AElfIndexer.DTOs;
using AElfIndexer.Processors;
using AElfIndexer.Grains.Grain.Blocks;
using AElfIndexer.Orleans.TestBase;
using AElfIndexer.Providers;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Modularity;

namespace AElfIndexer;

[DependsOn(
    typeof(AElfIndexerBlockChainEventHandlerCoreModule),
    typeof(AbpEventBusModule),
    typeof(AElfIndexerOrleansTestBaseModule),
    typeof(AElfIndexerDomainModule))]
public class AElfIndexerBlockChainEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<PrimaryKeyOptions>(o =>
        {
            o.BlockGrainSwitchInterval = 100;
        });
        
        context.Services.AddTransient<IDistributedEventHandler<BlockChainDataEto>>(sp =>
            sp.GetService<BlockChainDataEventHandler>());
        context.Services.AddSingleton<IBlockGrainProvider>(sp => sp.GetService<TestBlockGrainProvider>());
    }
}