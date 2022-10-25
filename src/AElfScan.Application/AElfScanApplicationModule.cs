using AElfScan.AElf;
using AElfScan.Orleans;
using AElfScan.ScanClients;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Configuration;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AElfScan;

[DependsOn(
    typeof(AElfScanDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(AElfScanApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class AElfScanApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options =>
        {
            options.AddMaps<AElfScanApplicationModule>();
        });
        
        var configuration = context.Services.GetConfiguration();
        Configure<ClusterOptions>(configuration.GetSection("Orleans:Cluster"));
        context.Services.AddSingleton<IClusterClient>(_ => _.GetService<IClusterClientAppService>()?.Client);
    }
}

