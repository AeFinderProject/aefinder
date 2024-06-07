using AeFinder.Apps;
using AeFinder.User;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder;

[DependsOn(
    typeof(AeFinderApplicationTestModule)
)]
public class AeFinderApplicationAppTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IOrganizationAppService, MockOrganizationAppService>();
    }
}
