using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Blocks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.Grains;

[DependsOn(typeof(AeFinderApplicationContractsModule))]
public class AeFinderGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<BlockPushOptions>(configuration.GetSection("BlockPush"));
        Configure<PrimaryKeyOptions>(configuration.GetSection("GrainPrimaryKey"));
        Configure<AppSettingOptions>(configuration.GetSection("AppSetting"));

        context.Services.AddSingleton<IBlockGrain, BlockGrain>();
    }
}