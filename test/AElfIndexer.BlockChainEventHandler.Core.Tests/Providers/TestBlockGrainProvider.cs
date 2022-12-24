using AElfIndexer.Grains;
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
            _blockBranchGrain = _clusterClient.GetGrain<IBlockBranchGrain>(
                GrainIdHelper.GenerateGrainId(chainId, AElfIndexerApplicationConsts.BlockBranchGrainIdSuffix));
        }

        return _blockBranchGrain;
    }

}