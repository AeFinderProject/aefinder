using AeFinder.Sdk.Processor;
using AeIndexerTemplate.GraphQL;
using AeIndexerTemplate.Processors;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AeIndexerTemplate;

public class AeIndexerTemplateModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AeIndexerTemplateModule>(); });
        context.Services.AddSingleton<ISchema, AeIndexerSchema>();
        
        // Add your LogEventProcessor implementation.
        //context.Services.AddSingleton<ILogEventProcessor, MyLogEventProcessor>();
    }
}