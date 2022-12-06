using AElfIndexer.Orleans.TestBase;
using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace AElfIndexer;

public abstract class AElfIndexerOrleansTestBase<TStartupModule>:AElfIndexerTestBase<TStartupModule> 
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public AElfIndexerOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}