using AElfScan.AElf.DTOs;
using AElfScan.AElf.Processors;
using AElfScan.Providers;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Modularity;

namespace AElfScan.Orleans.TestBase;

[DependsOn(typeof(AElfScanBlockChainEventHandlerCoreModule),
    typeof(AElfScanEntityEventHandlerCoreModule),
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AElfScanDomainModule)
)]
public class AElfScanOrleansTestBaseModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
    }
}