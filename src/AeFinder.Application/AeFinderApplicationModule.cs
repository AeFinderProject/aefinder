using System;
using AeFinder.AmazonCloud;
using AeFinder.ApiKeys;
using AeFinder.App.Deploy;
using AeFinder.Assets;
using AeFinder.Billings;
using AeFinder.BlockSync;
using AeFinder.CodeOps;
using AeFinder.CodeOps.Policies;
using AeFinder.DevelopmentTemplate;
using AeFinder.Email;
using AeFinder.Grains;
using AeFinder.Grains.Grain.ApiKeys;
using AeFinder.Metrics;
using AeFinder.Options;
using AeFinder.User;
using AElf.EntityMapping;
using AElf.EntityMapping.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.Emailing;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Timing;

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
    typeof(AeFinderCodeOpsModule),
    typeof(AeFinderAppDeployModule)
)]
public class AeFinderApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeFinderApplicationModule>(); });

        var configuration = context.Services.GetConfiguration();
        Configure<ApiOptions>(configuration.GetSection("Api"));
        Configure<BlockSyncOptions>(configuration.GetSection("BlockSync"));
        Configure<AppDeployOptions>(configuration.GetSection("AppDeploy"));
        Configure<DevTemplateOptions>(configuration.GetSection("DevTemplate"));
        Configure<AmazonS3Options>(configuration.GetSection("AmazonS3"));
        context.Services.AddTransient<ICodeAuditor, CodeAuditor>();
        context.Services.AddTransient<IPolicy, DefaultPolicy>();
        context.Services.Configure<SignatureGrantOptions>(configuration.GetSection("Signature"));
        context.Services.Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<AwsEmailOptions>(configuration.GetSection("AwsEmail"));
        context.Services.AddTransient<IBillingGenerator, AdvancePaymentBillingGenerator>();
        context.Services.AddTransient<IBillingGenerator, SettlementBillingGenerator>();
        context.Services.AddTransient<IResourceUsageProvider, ApiKeyUsageProvider>();
        context.Services.Configure<EmailTemplateOptions>(configuration.GetSection("EmailTemplate"));
        context.Services.Configure<AssetInitializationOptions>(configuration.GetSection("AssetInitialization"));
        
        Configure<AbpClockOptions>(options =>
        {
            options.Kind = DateTimeKind.Utc;
        });

        Configure<CustomOrganizationOptions>(configuration.GetSection("CustomOrganization"));
        context.Services.AddSingleton<IEmailSender, AwsEmailSender>();
    }
}