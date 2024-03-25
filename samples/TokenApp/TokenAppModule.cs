using AeFinder.Sdk.Processor;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using TokenApp.GraphQL;
using TokenApp.Processors;
using Volo.Abp.Modularity;

namespace TokenApp;

public class TokenAppModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<ISchema, TokenAppSchema>();
        context.Services.AddTransient<ILogEventProcessor, TokenTransferredProcessor>();
    }
}