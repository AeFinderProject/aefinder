using AeFinder.Sdk.Processor;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using TokenAeIndexer.GraphQL;
using TokenAeIndexer.Processors;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace TokenAeIndexer;

public class TokenAeIndexerModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<TokenAeIndexerModule>(); });
        context.Services.AddSingleton<ISchema, AeIndexerSchema>();
        
        context.Services.AddSingleton<ILogEventProcessor, TokenTransferredProcessor>();
    }
}