using AElfIndexer.Grains.Grain.BlockPush;
using AElfIndexer.Grains.Grain.Blocks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfIndexer.Grains;

[DependsOn(typeof(AElfIndexerApplicationContractsModule))]
public class AElfIndexerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockPushOptions>(configuration.GetSection("BlockScan"));
        Configure<PrimaryKeyOptions>(configuration.GetSection("GrainPrimaryKey"));

        context.Services.AddSingleton<IBlockGrain, BlockGrain>();
    }
}