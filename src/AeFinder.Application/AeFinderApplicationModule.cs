using AeFinder.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AeFinder;

[DependsOn(
    typeof(AeFinderDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(AeFinderApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AeFinderGrainsModule)
)]
public class AeFinderApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderApplicationModule>(); });

        var configuration = context.Services.GetConfiguration();
        Configure<ApiOptions>(configuration.GetSection("Api"));
    }
}

