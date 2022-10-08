using AElfScan.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(typeof(AElfScanBlockChainEventHandlerCoreModule),
    typeof(AbpEventBusModule),
    typeof(AElfScanDomainModule))]
public class AElfScanBlockChainEventHandlerCoreTestModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
    }
}