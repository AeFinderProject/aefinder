using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Blocks;
using AeFinder.Kubernetes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.Grains;

[DependsOn(typeof(AeFinderApplicationContractsModule), typeof(AeFinderKubernetesModule))]
public class AeFinderGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockPushOptions>(configuration.GetSection("BlockPush"));
        Configure<PrimaryKeyOptions>(configuration.GetSection("GrainPrimaryKey"));

        context.Services.AddSingleton<IBlockGrain, BlockGrain>();
    }
}