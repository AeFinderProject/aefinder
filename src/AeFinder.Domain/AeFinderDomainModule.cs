using AeFinder.MultiTenancy;
using AeFinder.OpenIddict;
using AeFinder.OpenIddict.Login;
using AElf.EntityMapping.Elasticsearch;
using AElf.ExceptionHandler.ABP;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenIddict.Abstractions;
using Volo.Abp.AuditLogging;
using Volo.Abp.Emailing;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.OpenIddict;
using Volo.Abp.PermissionManagement.Identity;
using Volo.Abp.PermissionManagement.OpenIddict;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace AeFinder;

[DependsOn(
    typeof(AeFinderDomainSharedModule),
    typeof(AbpAuditLoggingDomainModule),
    //typeof(AbpBackgroundJobsDomainModule),
    typeof(AbpFeatureManagementDomainModule),
    typeof(AbpIdentityDomainModule),
    typeof(AbpPermissionManagementDomainIdentityModule),
    typeof(AbpOpenIddictDomainModule),
    typeof(AbpPermissionManagementDomainOpenIddictModule),
    typeof(AbpSettingManagementDomainModule),
    typeof(AbpTenantManagementDomainModule),
    typeof(AbpEmailingModule),
    typeof(AElfEntityMappingElasticsearchModule),
    typeof(AOPExceptionModule)
)]
public class AeFinderDomainModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpMultiTenancyOptions>(options => { options.IsEnabled = MultiTenancyConsts.IsEnabled; });
        context.Services.TryAddTransient<ILoginNewUserCreator, LoginNewUserCreator>();

#if DEBUG
        context.Services.Replace(ServiceDescriptor.Singleton<IEmailSender, NullEmailSender>());
#endif
        //Override AbpOpenIddictTokenStore and set PruneAsync isTransactional as false
        var tokenStoreRootType = OpenIddictHelpers.FindGenericBaseType(typeof(AeFinderOpenIddictTokenStore), typeof(IOpenIddictTokenStore<>));
        context.Services.Replace(new ServiceDescriptor(typeof(IOpenIddictTokenStore<>).MakeGenericType(tokenStoreRootType.GenericTypeArguments[0]), typeof(AeFinderOpenIddictTokenStore), ServiceLifetime.Scoped));

        //Override AbpOpenIddictTokenStore and set PruneAsync isTransactional as false
        var authorizationStoreRootType = OpenIddictHelpers.FindGenericBaseType(typeof(AeFinderOpenIddictAuthorizationStore), typeof(IOpenIddictAuthorizationStore<>));
        context.Services.Replace(new ServiceDescriptor(typeof(IOpenIddictAuthorizationStore<>)
            .MakeGenericType(authorizationStoreRootType.GenericTypeArguments[0]), typeof(AeFinderOpenIddictAuthorizationStore), ServiceLifetime.Scoped));
    }
}