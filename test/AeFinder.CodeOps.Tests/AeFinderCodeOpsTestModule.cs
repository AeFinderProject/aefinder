using AeFinder.CodeOps.Validators.Assembly;
using AeFinder.CodeOps.Validators.Whitelist;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.CodeOps;

[DependsOn(
    typeof(AeFinderCodeOpsModule)
)]
public class AeFinderCodeOpsTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<AeFinderEntityValidator>();
        context.Services.AddTransient<WhitelistValidator>();
        
        Configure<CodeOpsOptions>(options =>
        {
            options.MaxEntityCount = 3;
        });
    }
}