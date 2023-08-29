using AElf.EntityMapping;
using AElf.EntityMapping.Elasticsearch;
using AElfIndexer.Grains;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AElfIndexer;

[DependsOn(
    typeof(AElfIndexerDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(AElfIndexerApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule),
    typeof(AElfIndexerGrainsModule),
    typeof(AElfEntityMappingModule),
    typeof(AElfEntityMappingElasticsearchModule)
)]
public class AElfIndexerApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AElfIndexerApplicationModule>(); });

        var configuration = context.Services.GetConfiguration();
        Configure<ApiOptions>(configuration.GetSection("Api"));
    }
}

