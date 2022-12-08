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

    // public async Task<IBlockGrain> GetBlockGrain(string chainId)
    // {
    //     if (_blockGrain == null)
    //     {
    //         _blockGrain = _clusterClient.GetGrain<IBlockGrain>("AELF_0");
    //     }
    //
    //     return _blockGrain;
    // }
    
    public async Task<IBlockGrain> GetBlockGrain(string chainId, string blockHash)
    {
        string primaryKey = chainId + AElfIndexerConsts.BlockGrainIdSuffix + blockHash;
        var newGrain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);

        return newGrain;
    }

    public async Task<bool> GrainExist(string chainId, string blockHash)
    {
        string primaryKey = chainId + AElfIndexerConsts.BlockGrainIdSuffix + blockHash;
        var grain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);

        var blockHeight = await grain.GetBlockHeight();

        return blockHeight > 0 ? true : false;
    }
}