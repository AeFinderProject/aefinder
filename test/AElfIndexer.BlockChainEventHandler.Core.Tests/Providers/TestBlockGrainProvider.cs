using AElfIndexer.Grains.Grain.Blocks;
using AElfIndexer.Orleans.TestBase;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Providers;

public class TestBlockGrainProvider: AElfIndexerTestBase<AElfIndexerOrleansTestBaseModule>,IBlockGrainProvider, ISingletonDependency
{
    private IClusterClient _clusterClient;
    private IBlockGrain _blockGrain;
    private IBlockBranchGrain _blockBranchGrain;

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
    
    // public async Task<IBlockGrain> GetBlockGrain(string chainId, string blockHash)
    // {
    //     string primaryKey = chainId + AElfIndexerConsts.BlockGrainIdSuffix + blockHash;
    //     var newGrain = _clusterClient.GetGrain<IBlockGrain>(primaryKey);
    //
    //     return newGrain;
    // }

    public async Task<IBlockBranchGrain> GetBlockBranchGrain(string chainId)
    {
        if (_blockBranchGrain == null)
        {
            string primaryKey = chainId + AElfIndexerConsts.BlockDictionaryGrainIdSuffix;
            _blockBranchGrain = _clusterClient.GetGrain<IBlockBranchGrain>(primaryKey);
        }

        return _blockBranchGrain;
    }

}