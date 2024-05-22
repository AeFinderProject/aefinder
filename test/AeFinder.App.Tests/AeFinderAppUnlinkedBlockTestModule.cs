using AeFinder.BlockScan;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.App;

[DependsOn(
    typeof(AeFinderAppTestModule)
)]
public class AeFinderAppUnlinkedBlockTestModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IBlockFilterAppService, MockBlockFilterAppService>();
    }
}