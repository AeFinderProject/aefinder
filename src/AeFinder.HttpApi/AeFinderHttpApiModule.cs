using AeFinder.Localization;
using AeFinder.Logger;
using Localization.Resources.AbpUi;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AeFinder;

[DependsOn(
    typeof(AeFinderApplicationContractsModule),
    typeof(AbpAccountHttpApiModule),
    typeof(AbpIdentityHttpApiModule),
    typeof(AbpPermissionManagementHttpApiModule),
    typeof(AbpTenantManagementHttpApiModule),
    typeof(AbpFeatureManagementHttpApiModule),
    typeof(AbpSettingManagementHttpApiModule),
    typeof(AbpAspNetCoreSignalRModule),
    typeof(AeFinderLogggerModule)
    )]
public class AeFinderHttpApiModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureLocalization();
        //Remove Account Controller
        Configure<MvcOptions>(options =>
        {
            options.Conventions.Add(new ApplicationDescription());
        });
    }

    private void ConfigureLocalization()
    {
        Configure<AbpLocalizationOptions>(options =>
        {
            options.Resources
                .Get<AeFinderResource>()
                .AddBaseTypes(
                    typeof(AbpUiResource)
                );
        });
    }
}
