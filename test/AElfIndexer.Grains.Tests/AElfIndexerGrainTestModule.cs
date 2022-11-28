using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Modularity;

namespace AElfIndexer.Grains;

[DependsOn(
    typeof(AElfIndexerDomainTestModule),
    typeof(AElfIndexerGrainsModule)
    )]
public class AElfIndexerGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
    }
}
