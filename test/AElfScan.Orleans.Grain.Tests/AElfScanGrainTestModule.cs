using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(
    typeof(AElfScanOrleansEventSourcingModule),
    typeof(AElfScanDomainTestModule),
    typeof(AElfScanOrleansEventSourcingModule)
    )]
public class AElfScanGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
    }
}
