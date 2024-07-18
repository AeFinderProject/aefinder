using System.Collections.Generic;
using AeFinder.Block;
using AeFinder.Grains.BlockPush;
using AeFinder.Grains.Grain.BlockPush;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp.Modularity;

namespace AeFinder.Grains;

[DependsOn(
    typeof(AeFinderDomainTestModule),
    typeof(AeFinderGrainsModule),
    typeof(AeFinderApplicationModule)
    )]
public class AeFinderGrainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IClusterClient>(sp => sp.GetService<ClusterFixture>().Cluster.Client);
        context.Services.AddSingleton<IBlockAppService, MockBlockAppService>();
    }
}
