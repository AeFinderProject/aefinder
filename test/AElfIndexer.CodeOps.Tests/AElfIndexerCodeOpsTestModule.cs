using AElfIndexer.CodeOps.Patchers.Module;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfIndexer.CodeOps.Tests;

[DependsOn(
    typeof(AElfIndexerCodeOpsModule)
)]
public class AElfIndexerCodeOpsTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<EntityIndexingPatcher>();
    }
}