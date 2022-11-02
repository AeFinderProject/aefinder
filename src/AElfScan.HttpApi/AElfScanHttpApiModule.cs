using System;
using System.Threading.Tasks;
using Localization.Resources.AbpUi;
using AElfScan.Localization;
using AElfScan.Orleans;
using AElfScan.Orleans.EventSourcing.Grain.Chains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
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
