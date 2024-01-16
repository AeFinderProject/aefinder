using Orleans.TestingHost;

namespace AeFinder.Grains;

public abstract class AeFinderGrainTestBase : AeFinderTestBase<AeFinderGrainTestModule>
{
    protected readonly TestCluster Cluster;

    public AeFinderGrainTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}
