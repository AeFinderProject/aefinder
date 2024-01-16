using AeFinder.BlockChainEventHandler.Core.DTOs;
using AeFinder.BlockChainEventHandler.Core.Processors;
using AeFinder.Grains.Grain.Blocks;
using AeFinder.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Modularity;

namespace AeFinder.BlockChainEventHandler.Core.Tests;

[DependsOn(
    typeof(AeFinderBlockChainEventHandlerCoreModule),
    typeof(AbpEventBusModule),
    typeof(AeFinderOrleansTestBaseModule),
    typeof(AeFinderDomainModule))]
public class AeFinderBlockChainEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<PrimaryKeyOptions>(o =>
        {
            o.BlockGrainSwitchInterval = 100;
        });
        
        context.Services.AddTransient<IDistributedEventHandler<BlockChainDataEto>>(sp =>
            sp.GetService<BlockChainDataEventHandler>());
    }
}