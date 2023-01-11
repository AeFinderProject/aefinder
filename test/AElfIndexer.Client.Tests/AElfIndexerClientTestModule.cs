using AElfIndexer.Client;
using AElfIndexer.Orleans.TestBase;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute.Extensions;
using Volo.Abp.Modularity;

namespace AElfIndexer;

[DependsOn(
    typeof(AElfIndexerClientModule),
    typeof(AElfIndexerDomainTestModule),
    typeof(AElfIndexerOrleansTestBaseModule)
    )]
public class AElfIndexerClientTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
    }
}
