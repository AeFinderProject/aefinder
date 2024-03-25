using AeFinder.CodeOps.Validators;
using AeFinder.CodeOps.Validators.Assembly;
using AeFinder.CodeOps.Validators.Whitelist;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeFinder.CodeOps;

public class AeFinderCodeOpsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IValidator, AeFinderEntityValidator>();
        context.Services.AddTransient<IValidator, WhitelistValidator>();
    }
}