using AElfIndexer.DTOs;
using AElfIndexer.Processors;
using AElfIndexer.Providers;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Modularity;

namespace AElfIndexer.Orleans.TestBase;

[DependsOn(typeof(AElfIndexerBlockChainEventHandlerCoreModule),
    typeof(AElfIndexerEntityEventHandlerCoreModule),
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AElfIndexerDomainModule)
)]
public class AElfIndexerOrleansTestBaseModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
    }
}