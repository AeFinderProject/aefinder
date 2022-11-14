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

    public async Task<IBlockGrain> GetBlockGrain(string chainId)
    {
        if (_blockGrain == null)
        {
            _blockGrain = _clusterClient.GetGrain<IBlockGrain>("AELF_0");
        }

        return _blockGrain;
    }
}