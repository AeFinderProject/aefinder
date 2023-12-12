using AElfIndexer.Grains.Grain.Blocks;
using AElfIndexer.Grains.Grain.BlockScan;
using AElfIndexer.Grains.Grain.Client;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfIndexer.Grains;

[DependsOn(typeof(AElfIndexerApplicationContractsModule))]
public class AElfIndexerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockScanOptions>(configuration.GetSection("BlockScan"));
        Configure<PrimaryKeyOptions>(configuration.GetSection("GrainPrimaryKey"));
        Configure<ClientOptions>(configuration.GetSection("Client"));

        context.Services.AddSingleton<IBlockGrain, BlockGrain>();
    }
}