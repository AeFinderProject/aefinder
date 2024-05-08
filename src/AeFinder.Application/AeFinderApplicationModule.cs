using AeFinder.BlockSync;
using AeFinder.CodeOps;
using AeFinder.CodeOps.Policies;
using AeFinder.Grains;
using AeFinder.Kubernetes;
using AeFinder.Kubernetes.Manager;
using AeFinder.Option;
using AElf.EntityMapping;
using AElf.EntityMapping.Elasticsearch;
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
    typeof(AeFinderGrainsModule),
    typeof(AElfEntityMappingModule),
    typeof(AElfEntityMappingElasticsearchModule),
    typeof(AeFinderCodeOpsModule)
)]
public class AeFinderApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderApplicationModule>(); });

        var configuration = context.Services.GetConfiguration();
        Configure<ApiOptions>(configuration.GetSection("Api"));
        Configure<BlockSyncOptions>(configuration.GetSection("BlockSync"));
        Configure<StudioOption>(configuration.GetSection("StudioOption"));
        Configure<AuthOption>(configuration.GetSection("AuthOption"));
        context.Services.AddTransient<ICodeAuditor, CodeAuditor>();
        context.Services.AddTransient<IPolicy, DefaultPolicy>();
    }
}