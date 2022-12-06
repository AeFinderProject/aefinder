using AElfIndexer.Grains.Grain.Blocks;
using AElfIndexer.Grains.Grain.BlockScan;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfIndexer.Grains;

[DependsOn(typeof(AElfIndexerDomainModule), typeof(AElfIndexerApplicationContractsModule))]
public class AElfIndexerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockScanOptions>(configuration.GetSection("BlockScan"));
        Configure<PrimaryKeyOptions>(configuration.GetSection("GrainPrimaryKey"));

        context.Services.AddSingleton<IBlockGrain, BlockGrain>();

        context.Services.AddTransient<IBlockFilterProvider, BlockFilterProvider>();
        context.Services.AddTransient<IBlockFilterProvider, TransactionFilterProvider>();
        context.Services.AddTransient<IBlockFilterProvider, LogEventFilterProvider>();
    }
}