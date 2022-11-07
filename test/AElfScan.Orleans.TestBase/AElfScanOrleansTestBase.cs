using AElfScan.Orleans.TestBase;
using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace AElfScan;

public abstract class AElfScanOrleansTestBase<TStartupModule>:AElfScanTestBase<TStartupModule> 
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public AElfScanOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}