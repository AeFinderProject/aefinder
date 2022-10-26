using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfScan.Orleans.TestBase;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(AElfScanDomainModule)
)]
public class AElfScanOrleansTestBaseModule:AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}