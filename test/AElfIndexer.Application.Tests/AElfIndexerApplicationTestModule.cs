using System.Collections.Generic;
using AElf.EntityMapping;
using AElf.EntityMapping.Elasticsearch;
using AElf.EntityMapping.Options;
using AElf.EntityMapping.Sharding;
using AElfIndexer.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.Extensions;
using Volo.Abp.Modularity;

namespace AElfIndexer;

[DependsOn(
    typeof(AElfIndexerApplicationModule),
    typeof(AElfIndexerDomainTestModule),
    typeof(AElfIndexerOrleansTestBaseModule),
    typeof(AElfEntityMappingElasticsearchModule)
)]
public class AElfIndexerApplicationTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Configure<ApiOptions>(o =>
        {
            o.BlockQueryHeightInterval = 1000;
            o.TransactionQueryHeightInterval = 1000;
            o.LogEventQueryHeightInterval = 1000;
        });
    }
}
