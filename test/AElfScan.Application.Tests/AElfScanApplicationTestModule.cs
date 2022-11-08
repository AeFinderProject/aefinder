using Microsoft.Extensions.DependencyInjection;
using NSubstitute.Extensions;
using Volo.Abp.Modularity;

namespace AElfScan;

[DependsOn(
    typeof(AElfScanApplicationModule),
    typeof(AElfScanDomainTestModule)
    )]
public class AElfScanApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {

        context.Services.Configure<ApiOptions>(o =>
        {
            o.BlockQueryAmountInterval = 1000;
        });

    }
}
