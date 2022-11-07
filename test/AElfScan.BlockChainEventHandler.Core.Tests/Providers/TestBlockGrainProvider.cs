using AElfScan.Grains.Grain;
using AElfScan.Orleans.TestBase;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfScan.Providers;

public class TestBlockGrainProvider: AElfScanTestBase<AElfScanOrleansTestBaseModule>,IBlockGrainProvider, ISingletonDependency
{
    private IClusterClient _clusterClient;
    private IBlockGrain _blockGrain;

    public TestBlockGrainProvider()
    {
        _clusterClient = GetRequiredService<ClusterFixture>().Cluster.Client;
    }
    
    public IBlockGrain GetBlockGrain()
    {
        if (_blockGrain == null)
        {
            _blockGrain = _clusterClient.GetGrain<IBlockGrain>(1);
        }

        return _blockGrain;
    }
}