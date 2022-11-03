using Orleans.TestingHost;

namespace AElfScan.Grains;

public abstract class AElfScanGrainTestBase : AElfScanTestBase<AElfScanGrainTestModule>
{
    protected readonly TestCluster Cluster;

    public AElfScanGrainTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}
