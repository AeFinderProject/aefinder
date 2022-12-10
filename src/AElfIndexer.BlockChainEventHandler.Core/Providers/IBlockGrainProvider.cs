using System.Threading.Tasks;
using AElfIndexer.Grains.Grain.Blocks;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AElfIndexer.Providers;

public interface IBlockGrainProvider
{
    Task<IBlockBranchGrain> GetBlockBranchGrain(string chainId);
}

public class BlockGrainProvider : IBlockGrainProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;

    public BlockGrainProvider(IClusterClient clusterClient)
    {
        _clusterClient = clusterClient;
    }

    public async Task<IBlockBranchGrain> GetBlockBranchGrain(string chainId)
    {
        string primaryKey = chainId + AElfIndexerApplicationConsts.BlockDictionaryGrainIdSuffix;
        var grain = _clusterClient.GetGrain<IBlockBranchGrain>(primaryKey);

        return grain;
    }
}