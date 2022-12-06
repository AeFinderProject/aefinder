using AElfIndexer.Grains.Grain.Blocks;
using AElfIndexer.Orleans.TestBase;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Providers;

public class TestBlockGrainProvider: AElfIndexerTestBase<AElfIndexerOrleansTestBaseModule>,IBlockGrainProvider, ISingletonDependency
{
    private IClusterClient _clusterClient;
    private IBlockGrain _blockGrain;

    public TestBlockGrainProvider()
    {
        _clusterClient = GetRequiredService<ClusterFixture>().Cluster.Client;
    }

    public async Task<IBlockGrain> GetBlockGrain(string chainId)
    {
        if (_blockGrain == null)
        {
            _blockGrain = _clusterClient.GetGrain<IBlockGrain>("AELF_0");
        }

        return _blockGrain;
    }
}