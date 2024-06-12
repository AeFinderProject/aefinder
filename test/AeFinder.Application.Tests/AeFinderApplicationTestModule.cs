using AeFinder.CodeOps;
using AeFinder.Orleans.TestBase;
using AElf.EntityMapping.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AeFinder;

[DependsOn(
    typeof(AeFinderApplicationModule),
    typeof(AeFinderDomainTestModule),
    typeof(AeFinderOrleansTestBaseModule),
    typeof(AElfEntityMappingElasticsearchModule)
)]
public class AeFinderApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<ApiOptions>(o =>
        {
            o.BlockQueryHeightInterval = 1000;
            o.TransactionQueryHeightInterval = 1000;
            o.LogEventQueryHeightInterval = 1000;
        });
        
        context.Services.AddTransient<ICodeAuditor>(o=>Mock.Of<ICodeAuditor>());
    }
}
