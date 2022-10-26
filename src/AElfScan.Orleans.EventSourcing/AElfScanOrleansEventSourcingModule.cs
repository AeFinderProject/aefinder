using AElfScan.Grain;
using AElfScan.Orleans.EventSourcing.Grain.BlockScan;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(typeof(AElfScanDomainModule), typeof(AElfScanApplicationContractsModule))]
public class AElfScanOrleansEventSourcingModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockScanOptions>(configuration.GetSection("BlockScan"));

        context.Services.AddSingleton<IBlockGrain, BlockGrain>();
    }
}