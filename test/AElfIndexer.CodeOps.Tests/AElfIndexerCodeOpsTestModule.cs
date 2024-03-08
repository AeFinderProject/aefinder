using AElfIndexer.CodeOps.Validators.Assembly;
using AElfIndexer.CodeOps.Validators.Whitelist;
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
        context.Services.AddTransient<IndexerEntityValidator>();
        context.Services.AddTransient<WhitelistValidator>();
        
        Configure<CodeOpsOptions>(options =>
        {
            options.MaxEntityCount = 3;
        });
    }
}