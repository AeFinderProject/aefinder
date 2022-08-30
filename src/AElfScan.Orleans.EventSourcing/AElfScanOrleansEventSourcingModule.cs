using AElfScan.Grain;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfScan;

public class AElfScanOrleansEventSourcingModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IBlockGrain, BlockGrain>();
    }
}