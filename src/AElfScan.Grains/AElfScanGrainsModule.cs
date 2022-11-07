using AElfScan.Grains.Grain;
using AElfScan.Grains.Grain.BlockScan;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfScan.Grains;

[DependsOn(typeof(AElfScanDomainModule), typeof(AElfScanApplicationContractsModule))]
public class AElfScanGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockScanOptions>(configuration.GetSection("BlockScan"));

        context.Services.AddSingleton<IBlockGrain, BlockGrain>();

        context.Services.AddTransient<IBlockFilterProvider, BlockFilterProvider>();
        context.Services.AddTransient<IBlockFilterProvider, TransactionFilterProvider>();
        context.Services.AddTransient<IBlockFilterProvider, LogEventFilterProvider>();
    }
}