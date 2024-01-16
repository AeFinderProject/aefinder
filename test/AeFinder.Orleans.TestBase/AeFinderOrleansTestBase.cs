using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace AeFinder.Orleans.TestBase;

public abstract class AeFinderOrleansTestBase<TStartupModule>:AeFinderTestBase<TStartupModule> 
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public AeFinderOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}