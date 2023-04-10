using Orleans.TestingHost;

namespace AElfIndexer.Grains;

public abstract class AElfIndexerGrainTestBase : AElfIndexerTestBase<AElfIndexerGrainTestModule>
{
    protected readonly TestCluster Cluster;

    public AElfIndexerGrainTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}
