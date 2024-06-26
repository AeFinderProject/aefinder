using AeFinder.Sdk.Processor;
using AppTemplate.GraphQL;
using AppTemplate.Processors;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AppTemplate;

public class AppTemplateModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<AppTemplateModule>(); });
        context.Services.AddSingleton<ISchema, AppSchema>();
        
        // Add your LogEventProcessor implementation.
        //context.Services.AddTransient<ILogEventProcessor, MyLogEventProcessor>();
    }
}