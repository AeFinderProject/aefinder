using AeFinder.App.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AeIndexerTemplate;

[DependsOn(
    typeof(AeFinderAppTestBaseModule),
    typeof(AeIndexerTemplateModule))]
public class AeIndexerTemplateTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AeFinderAppEntityOptions>(options => { options.AddTypes<AeIndexerTemplateModule>(); });
        
        // Add your Processors.
        // context.Services.AddSingleton<MyLogEventProcessor>();
    }
}