using AeFinder.Grains.Grain.Blocks;
using AeFinder.Grains.Grain.BlockScan;
using AeFinder.Grains.Grain.Client;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.Grains;

[DependsOn(typeof(AeFinderApplicationContractsModule))]
public class AeFinderGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockScanOptions>(configuration.GetSection("BlockScan"));
        Configure<PrimaryKeyOptions>(configuration.GetSection("GrainPrimaryKey"));
        Configure<ClientOptions>(configuration.GetSection("Client"));

        context.Services.AddSingleton<IBlockGrain, BlockGrain>();

        context.Services.AddTransient<IBlockFilterProvider, BlockFilterProvider>();
        context.Services.AddTransient<IBlockFilterProvider, TransactionFilterProvider>();
        context.Services.AddTransient<IBlockFilterProvider, LogEventFilterProvider>();
    }
}