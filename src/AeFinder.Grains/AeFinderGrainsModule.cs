using AeFinder.Grains.Grain.ApiKeys;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Grains.Grain.Assets;
using AeFinder.Grains.Grain.BlockPush;
using AeFinder.Grains.Grain.Blocks;
using AeFinder.Grains.Grain.Orders;
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
        Configure<AppSettingOptions>(configuration.GetSection("AppSetting"));
        Configure<ApiKeyOptions>(configuration.GetSection("ApiKey"));

        context.Services.AddSingleton<IBlockGrain, BlockGrain>();
        
        context.Services.AddSingleton<IOrderValidationProvider, AppOrderValidationProvider>();
        context.Services.AddSingleton<IOrderValidationProvider, AssetOrderValidationProvider>();
        context.Services.AddSingleton<IOrderHandler, AppOrderHandler>();
        context.Services.AddSingleton<IOrderHandler, AssetOrderHandler>();
    }
}