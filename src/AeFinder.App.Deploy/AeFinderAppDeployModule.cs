using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.App.Deploy;

public class AeFinderAppDeployModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var services = context.Services;
        services.AddTransient(typeof(IAppDeployManager), typeof(DefaultAppDeployManager));
    }
}