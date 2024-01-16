using AeFinder.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder;

[DependsOn(
    typeof(AeFinderApplicationModule),
    typeof(AeFinderDomainTestModule),
    typeof(AeFinderOrleansTestBaseModule)
    )]
public class AeFinderApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {

        context.Services.Configure<ApiOptions>(o =>
        {
            o.BlockQueryHeightInterval = 1000;
            o.TransactionQueryHeightInterval = 1000;
            o.LogEventQueryHeightInterval = 1000;
        });

    }
}
