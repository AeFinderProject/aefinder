using AeFinder.BlockChainEventHandler;
using AeFinder.EntityEventHandler;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AeFinder.Orleans.TestBase;

[DependsOn(typeof(AeFinderBlockChainEventHandlerCoreModule),
    typeof(AeFinderEntityEventHandlerCoreModule),
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AeFinderDomainModule)
)]
public class AeFinderOrleansTestBaseModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
    }
}