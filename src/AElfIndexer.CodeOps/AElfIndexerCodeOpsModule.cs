using AElfIndexer.CodeOps.Validators;
using AElfIndexer.CodeOps.Validators.Assembly;
using AElfIndexer.CodeOps.Validators.Whitelist;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfIndexer.CodeOps;

public class AElfIndexerCodeOpsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddTransient<IValidator, IndexerEntityValidator>();
        context.Services.AddTransient<IValidator, WhitelistValidator>();
    }
}