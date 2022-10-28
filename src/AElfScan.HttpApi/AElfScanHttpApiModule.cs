using System;
using System.Threading.Tasks;
using Localization.Resources.AbpUi;
using AElfScan.Localization;
using AElfScan.Orleans;
using AElfScan.Orleans.EventSourcing.Grain.Chains;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Threading;

namespace AElfScan;

[DependsOn(
    typeof(AElfScanApplicationContractsModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AbpTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpAspNetCoreSignalRModule)
    )]
public class AElfScanHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var clientService = context.ServiceProvider.GetRequiredService<IClusterClientAppService>();
        AsyncHelper.RunSync(async () => await clientService.StartAsync());
        
        // TODO: Delete it after debug     
        var client = clientService.Client;
        AsyncHelper.RunSync(async () => await DoClientWork(client));
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var clientService = context.ServiceProvider.GetService<IClusterClientAppService>();
        AsyncHelper.RunSync(() => clientService.StopAsync());
    }
    
    private static async Task DoClientWork(IClusterClient client)
    {
        var chainId = "AELF";
        var chainGrain = client.GetGrain<IChainGrain>(chainId);
        await chainGrain.SetLatestBlockAsync("Hash100",500);
        await chainGrain.SetLatestConfirmBlockAsync("Hash100",500);
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<AElfScanResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }
}
