using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Modularity;

namespace AElfScan.Grains;

[DependsOn(
    typeof(AElfScanDomainTestModule),
    typeof(AElfScanGrainsModule)
    )]
public class AElfScanGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
    }
}
